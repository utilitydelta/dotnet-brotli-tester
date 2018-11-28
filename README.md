# Brotli stream concatenation

You are writing a backend that receives chunks of a large file bit by bit as the client uploads. Can you Brotli compress each chunk and append it to the end of the file you are building on the server side?

NO!

Brotli does not write enough metadata to know when one frame ends and the next begins. So you either need to write one file per chunk on the server, or write the start and end position for each frame you have written to the file, and decompress each chunk one by one.

Gzip also works this way. Snappy is the only one I've found that works ok with concatenation of compressed streams. So be careful. :)

