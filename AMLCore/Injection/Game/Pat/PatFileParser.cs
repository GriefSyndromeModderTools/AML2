using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Pat
{
    internal class PatFileParser
    {
        public static void Modify(byte[] data, int offset)
        {
            int pos = 0;

            pos += 1;
            {
                int imageCount = BitConverter.ToInt16(data, pos);
                pos += 2;

                for (int i = 0; i < imageCount; ++i)
                {
                    pos += 0x80;
                }
            }
            {
                int animationCount = BitConverter.ToInt32(data, pos);
                pos += 4;

                for (int i = 0; i < animationCount; ++i)
                {
                    SkipAnimation(data, ref pos, offset);

                    //sometimes the stream ends before getting all the animations
                    if (pos >= data.Length)
                    {
                        break;
                    }
                }
            }
        }

        private static void SkipAnimation(byte[] data, ref int pos, int offset)
        {
            int animationID = BitConverter.ToInt32(data, pos);
            pos += 4;
            if (animationID == -1)
            {
                pos += 8;
                return;
            }
            if (animationID < 0)
            {
                offset = 0;
            }
            Buffer.BlockCopy(BitConverter.GetBytes(animationID + offset), 0, data, pos - 4, 4);

            pos += 5;

            var frameCount = BitConverter.ToInt32(data, pos);
            pos += 4;
            for (int i = 0; i < frameCount; ++i)
            {
                SkipFrame(data, ref pos);
            }
        }

        private static void SkipFrame(byte[] data, ref int pos)
        {
            pos += 18;
            SkipIM(data, ref pos);

            pos += 49;

            SkipPhysics(data, ref pos);

            {
                int hitCount = data[pos++];
                for (int i = 0; i < hitCount; ++i)
                {
                    SkipBox(ref pos);
                }
            }
            {
                int attackCount = data[pos++];
                for (int i = 0; i < attackCount; ++i)
                {
                    SkipBox(ref pos);
                }
            }

            SkipPoint(ref pos);
            SkipPoint(ref pos);
            SkipPoint(ref pos);
            pos += 6;
        }

        private static void SkipIM(byte[] data, ref int pos)
        {
            if (data[pos++] != 2)
            {
                return;
            }

            pos += 16;
        }

        private static void SkipPhysics(byte[] data, ref int pos)
        {
            if (data[pos++] == 0)
            {
                return;
            }

            pos += 16;
        }

        private static void SkipBox(ref int pos)
        {
            pos += 18;
        }

        private static void SkipPoint(ref int pos)
        {
            pos += 8;
        }
    }
}
