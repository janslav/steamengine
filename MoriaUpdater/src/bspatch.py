from sys import argv

import bz2
import struct
import cStringIO as StringIO

def main():
	old_file_name   = argv[1]
	new_file_name   = argv[2]
	patch_file_name = argv[3]

	patch_file = open(patch_file_name)

	raw_data = patch_file.read()

	(control_block_length, diff_block_length, new_file_size) = struct.unpack("qqq", raw_data[8:16] + raw_data[16:24] + raw_data[24:32])

	compressed_control_block = raw_data[32:32 + control_block_length]
	compressed_diff_block    = raw_data[32 + control_block_length:32 + control_block_length + diff_block_length]
	compressed_extra_block   = raw_data[32 + control_block_length + diff_block_length:]

	control_block = StringIO.StringIO(bz2.decompress(compressed_control_block))
	diff_block    = StringIO.StringIO(bz2.decompress(compressed_diff_block))
	extra_block   = StringIO.StringIO(bz2.decompress(compressed_extra_block))

	old_file      = open(old_file_name)
	old_file_size = len(old_file.read())
	old_file.seek(0)

	control_block_size = len(control_block.read())
	control_block.seek(0)

	block_size = 8
	new_file = open(new_file_name, "w")

	for i in range(control_block_size)[::3 * block_size]:
		# read control data
		# 1. add x bytes from old to x bytes of diff file
		# 2. copy y bytes from extra
		# 4. seek forward z bytes in the old file
		(x, y, z) = struct.unpack("qqq", control_block.read(8) + control_block.read(8) + control_block.read(8))
		diff = diff_block.read(x)
		old = old_file.read(x)
		test += len(old)
		for i in range(len(diff)):
			(a, b) = struct.unpack("BB", diff[i] + old[i])
			(result,) = struct.pack("B", (a + b) % 256)
			new_file.write(result)
		for i in extra_block.read(y):
			new_file.write(i)
		if z > 0:
			old_file.read(z)
	return new_file

if(len(argv) < 4):
	print "error: inputs"
else:
	new_file = main()
