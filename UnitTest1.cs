using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Xunit;

namespace DotnetCoreBrotliTester
{
    public class UnitTest1
    {
        private static Stream ExtractResource(string filename)
        {
            var a = Assembly.GetExecutingAssembly();
            return a.GetManifestResourceStream(filename);
        }

        [Theory]
        [InlineData(50000)]
        [InlineData(500000)]
        [InlineData(5000000)]
        public void TestBrotliChunkDecompressOk(int chunkSize)
        {
            var origStream = ExtractResource("DotnetCoreBrotliTester.image.jpg");

            var compressedStream = new MemoryStream();
            var compressedBoundaries = new List<(long start, long end)>();

            while (origStream.Position < origStream.Length)
            {
                var size = origStream.Length - origStream.Position;
                if (size > chunkSize) size = chunkSize;
                var bytes = new byte[size];
                origStream.Read(bytes, 0, bytes.Length);

                var startPos = compressedStream.Position;
                using (var compressor = new BrotliStream(compressedStream, CompressionMode.Compress, true))
                {
                    compressor.Write(bytes, 0, bytes.Length);
                }

                var endPos = compressedStream.Position;
                compressedBoundaries.Add((startPos, endPos));

            }

            var decompressedOriginal = new MemoryStream();
            compressedStream.Position = 0;
            foreach (var compressedBoundary in compressedBoundaries)
            {
                var compressedBytes = new byte[compressedBoundary.end-compressedBoundary.start];
                compressedStream.Read(compressedBytes, 0, compressedBytes.Length);
                using (var chunk = new MemoryStream(compressedBytes))
                using (var decompressor = new BrotliStream(chunk, CompressionMode.Decompress, true))
                {
                    decompressor.CopyTo(decompressedOriginal);
                }
            }

            decompressedOriginal.Position = 0;
            origStream.Position = 0;

            using (var fileTest = File.OpenWrite($"brotli-working-image-{chunkSize}.jpg"))
            {
                decompressedOriginal.CopyTo(fileTest);
            }

            decompressedOriginal.Position = 0;

            Assert.True(CompareStreams(origStream, decompressedOriginal));
        }

        [Theory]
        [InlineData(50000)]
        [InlineData(500000)]
        [InlineData(5000000)]
        public void TestBrotliNoChunkDecompressFail(int chunkSize)
        {
            var origStream = ExtractResource("DotnetCoreBrotliTester.image.jpg");

            var compressedStream = new MemoryStream();

            while (origStream.Position < origStream.Length)
            {
                var size = origStream.Length - origStream.Position;
                if (size > chunkSize) size = chunkSize;
                var bytes = new byte[size];
                origStream.Read(bytes, 0, bytes.Length);

                using (var compressor = new BrotliStream(compressedStream, CompressionMode.Compress, true))
                {
                    compressor.Write(bytes, 0, bytes.Length);
                }
            }

            var decompressedOriginal = new MemoryStream();
            compressedStream.Position = 0;
            using (var decompressor = new BrotliStream(compressedStream, CompressionMode.Decompress, true))
            {
                decompressor.CopyTo(decompressedOriginal);
            }

            decompressedOriginal.Position = 0;
            origStream.Position = 0;

            using (var fileTest = File.OpenWrite($"brotli-fail-image-{chunkSize}.jpg"))
            {
                decompressedOriginal.CopyTo(fileTest);
            }

            decompressedOriginal.Position = 0;

            if (chunkSize <= 500000)
            {
                Assert.False(CompareStreams(origStream, decompressedOriginal));
            }
            else
            {
                Assert.True(CompareStreams(origStream, decompressedOriginal));
            }
        }

        private bool CompareStreams(Stream a, Stream b)
        {
            if (a == null &&
                b == null)
                return true;
            if (a == null ||
                b == null)
            {
                throw new ArgumentNullException(
                    a == null ? "a" : "b");
            }

            if (a.Length < b.Length)
                return false;
            if (a.Length > b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                int aByte = a.ReadByte();
                int bByte = b.ReadByte();
                if (aByte.CompareTo(bByte) != 0)
                    return false;
            }

            return true;
        }
    }
}
