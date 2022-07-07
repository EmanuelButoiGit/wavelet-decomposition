using System;
using System.IO;

namespace Wavelet
{
    class BitWriter : IDisposable
    {
		private int ctBits;
		private byte writeBuffer;
		private BinaryWriter binaryWriter;
		private FileStream myFile;

		public BitWriter(string fileName)
		{
			myFile = new FileStream(fileName, FileMode.OpenOrCreate);
			binaryWriter = new BinaryWriter(myFile);
		}

		private bool IsBufferEmpty()
		{

			if (ctBits == 0)
			{
				return true;
			}

			else
			{
				return false;
			}

		}

		private bool IsBufferFull()
		{

			if (ctBits == 8)
			{
				return true;
			}

			else
			{
				return false;
			}

		}

		public void WriteBit(int bit)
		{

			writeBuffer = (byte)(writeBuffer << 1);
			bit = bit & 0x01; //?
			writeBuffer = (byte)(bit | writeBuffer);

			ctBits++;

			if (IsBufferFull())
			{
				myFile.WriteByte(writeBuffer);
				ctBits = 0;
			}
		}

		public void WriteNBits(int bits, int nrOfBits)
        {
			if (nrOfBits > 32)
			{
				throw new Exception("The number of bits is greater than 32! Please reintroduce the nr. again");
			}

			bits = bits << (32 - nrOfBits);

            for (int i = 0; i < nrOfBits; i++)
            {
				byte bit = (byte)((0x80000000 & bits) >> 31); //?
				WriteBit(bit);
				bits = bits << 1;
            }

		}

		private void ClearBuffer()
        {
            if (!IsBufferEmpty())
            {
				WriteNBits(0, 7);
            }
        }

		public void Dispose()
		{
			binaryWriter.Dispose();
		}
	}
}
