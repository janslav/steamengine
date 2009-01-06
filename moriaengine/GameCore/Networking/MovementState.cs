/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Common;
using System.IO;
using System.Net;
using SteamEngine.Regions;

namespace SteamEngine.Networking {

	//oddeleno od gamestate pro prehlednost
	public class MovementState {
		//These numbers are based on me running/walking/etc in game and recording the "Time between movement"
		//output of the movement info stuff. They're slightly higher than the highest values I saw,
		//which means that like this they should slow all clients to the same rate, so perhaps different
		//clients with different graphics-drawing speeds will actually move at the same rates instead of
		//at different ones. Or maybe not, since I have a pretty good graphics card. (SL)
		private readonly long RunStepTime = HighPerformanceTimer.SecondsToTicks(0.18);
		//private readonly long WalkStepTime = HighPerformanceTimer.SecondsToTicks(0.35);
		private readonly long RidingRunStepTime = HighPerformanceTimer.SecondsToTicks(0.09);
		//private readonly long RidingWalkStepTime = HighPerformanceTimer.SecondsToTicks(0.18);

		private static LinkedList<MovementState> queue = new LinkedList<MovementState>();
		private static Thread thread;
		private static ManualResetEvent manualResetEvent = new ManualResetEvent(false);

		private readonly LinkedListNode<MovementState> listNode;

		private readonly SimpleQueue<byte> moveRequests = new SimpleQueue<byte>();
		private long lastStepReserve;
		private long secondLastStepReserve;
		private long thirdLastStepReserve;
		private long lastMovementTime;
		private long nextMovementTime;

		private byte movementSequenceOut;
		private byte movementSequenceIn;
		//private byte altMovementSequenceIn;

		private GameState gameState;


		static MovementState() {
			thread = new Thread(Cycle);
			thread.IsBackground = true;
			thread.Start();
		}

		static void Cycle() {
			while (manualResetEvent.WaitOne()) {
				lock (queue) {
					LinkedListNode<MovementState> currentNode = queue.First;
					while (currentNode != null) {
						LinkedListNode<MovementState> nextNode = currentNode.Next;

						MovementState ms = currentNode.Value;
						if (ms.moveRequests.Count > 0) {
							lock (MainClass.globalLock) {
								ms.ProcessMovement(ms.moveRequests.Dequeue());
							}
							if (ms.moveRequests.Count == 0) {
								queue.Remove(currentNode);
							}
						} else {
							queue.Remove(currentNode);
						}

						currentNode = nextNode;
					}

					if (queue.Count == 0) {
						manualResetEvent.Reset();
					}
				}

				Thread.Sleep(5);
			}
		}


		internal MovementState(GameState gameState) {
			this.listNode = new LinkedListNode<MovementState>(this);

			this.gameState = gameState;
		}

		internal void On_Reset() {
			this.lastStepReserve = 0;
			this.secondLastStepReserve = 0;
			this.thirdLastStepReserve = 0;
			this.lastMovementTime = 0;
			this.nextMovementTime = 0;
			this.movementSequenceOut = 0;
			this.movementSequenceIn = 0;
			this.moveRequests.Clear();
		}

		internal void HandleMoveRequest(TCPConnection<GameState> conn, GameState state, byte direction, byte sequence) {
			if (this.CheckMovSeqInWithMsg(sequence)) {
				this.MovementRequest(direction);
			} else {
				CharMoveRejectionOutPacket packet = Pool<CharMoveRejectionOutPacket>.Acquire();
				packet.Prepare(sequence, state.CharacterNotNull);
				conn.SendSinglePacket(packet);
				this.ResetMovementSequence();
			}
		}

		internal bool CheckMovSeqInWithMsg(byte seq) {
			if (seq == this.movementSequenceIn) {
				return true;
			//} else if (seq == this.altMovementSequenceIn) {
			//    Logger.WriteDebug("Invalid seqNum " + LogStr.Number(seq) + ", expecting " + LogStr.Number(this.movementSequenceIn) + ", alternative worked");
			//    this.movementSequenceOut = seq;
			//    return true;
			} else {
				Logger.WriteError("Invalid seqNum " + LogStr.Number(seq) + ", expecting " + LogStr.Number(this.movementSequenceIn) + ".");// or " + LogStr.Number(this.altMovementSequenceIn));
				return false;
			}
		}

		//called from incomingpacket.Handle, so we're also under lock(globallock)
		internal void MovementRequest(byte dir) {
			//iterating incoming sequence number
			if (this.movementSequenceIn == 255) {
				//Logger.WriteInfo(MovementTracingOn, "reqMoveSeqNum wraps around to 1.");
				this.movementSequenceIn = 1;
			} else {
				//Logger.WriteInfo(MovementTracingOn, "reqMoveSeqNum gets increased.");
				this.movementSequenceIn++;
			}

			lock (queue) {
				int requestCount = moveRequests.Count;
				if (requestCount == 0) {
					this.ProcessMovement(dir);
				} else {
					this.moveRequests.Enqueue(dir);
				}
				if ((requestCount == 0) && (moveRequests.Count == 1)) {
					queue.AddFirst(this.listNode);
					manualResetEvent.Set();
				}
			}
		}

		private void ProcessMovement(byte dir) {
			AbstractCharacter ch = this.gameState.Character;
			if (ch == null) {
				this.moveRequests.Clear();
				return;
			}

			bool running = ((dir & 0x80) == 0x80);
			Direction direction = (Direction) (dir & 0x07);

			if (direction != ch.Direction) {//no speedcheck if we're just changing direction
				if (moveRequests.Count == 0) {
					this.Movement(direction, running);
				}
				return;
			}

			if ((this.CanMoveAgain()) || (this.gameState.Account.PLevel >= Globals.plevelOfGM)) {
				this.Movement(direction, running);
			} else {
				this.moveRequests.Enqueue(dir);
			}
		}

		private bool CanMoveAgain() {
			long currentTime = HighPerformanceTimer.TickCount;
			long diff = currentTime - nextMovementTime;

			//Logger.WriteInfo(MovementTracingOn, "Time between movement = "+HighPerformanceTimer.TicksToSeconds(currentTime-lastMovementTime));

			long reserves = lastStepReserve + secondLastStepReserve + thirdLastStepReserve;

			if (diff + reserves >= 0) {
				//Logger.WriteInfo(MovementTracingOn, "Time later than allowed = "+HighPerformanceTimer.TicksToSeconds(diff + reserves));

				if (this.gameState.CharacterNotNull.Flag_Riding) {
					nextMovementTime = (currentTime + RidingRunStepTime);
				} else {
					nextMovementTime = (currentTime + RunStepTime);
				}
				thirdLastStepReserve = secondLastStepReserve;
				secondLastStepReserve = lastStepReserve;
				lastStepReserve = Math.Max(diff, 0);
				lastMovementTime = currentTime;//...because sometimes the client tends to send more steps at once (or it looks like that), but it still isn't speedhacking
				return true;
			} else {
				////Logger.WriteInfo(MovementTracingOn, "Delaying movement, the packet is "+seconds+" seconds early.");
				//if (MovementTracingOn) { 
				//    //we need not spam the console with warnings. Players simply can't speedhack, end of story.
				//    Logger.WriteWarning("The client "+LogStr.Ident(this)+" is moving too fast. The packet came "+
				//        LogStr.Number(HighPerformanceTimer.TicksToSeconds(-(diff + reserves)))+" s earlier than allowed.");
				//}
				return false;
			}
		}

		private void Movement(Direction dir, bool running) {
			AbstractCharacter ch = this.gameState.CharacterNotNull;

			//Logger.WriteInfo(MovementTracingOn, "Moving.");
			bool success = ch.WalkRunOrFly((Direction) dir, running, true);	//true = we don't want to get a 0x77 for ourself, our client knows we're moving if it gets the verify move packet.
			if (success) {
				CharacterMoveAcknowledgeOutPacket packet = Pool<CharacterMoveAcknowledgeOutPacket>.Acquire();
				packet.Prepare(this.movementSequenceOut, ch.GetHighlightColorFor(ch));
				this.gameState.Conn.SendSinglePacket(packet);
				CharSyncQueue.ProcessChar(ch);//I think this is really needed. We can't wait to the end of the cycle, because movement 
				//should be as much synced between clients as possible

				if (this.movementSequenceOut == 255) {
				    //Logger.WriteInfo(MovementTracingOn, "moveSeqNum wraps around to 1.");
				    this.movementSequenceOut = 1;
				} else {
				    this.movementSequenceOut++;
				}
			} else {

				CharMoveRejectionOutPacket packet = Pool<CharMoveRejectionOutPacket>.Acquire();
				packet.Prepare(this.movementSequenceOut, ch);
				this.gameState.Conn.SendSinglePacket(packet);

				this.ResetMovementSequence();
				return;
			}
		}

		internal void ResetMovementSequence() {
			this.movementSequenceOut = 0;
			//this.altMovementSequenceIn = this.movementSequenceIn;
			this.movementSequenceIn = 0;
			this.moveRequests.Clear();
		}
	}
}