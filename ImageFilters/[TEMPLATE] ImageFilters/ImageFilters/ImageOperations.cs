using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace ImageFilters
{
    public class ImageOperations
    {
        /// <summary>
        /// Open an image, convert it to gray scale and load it into 2D array of size (Height x Width)
        /// </summary>
        /// <param name="ImagePath">Image file path</param>
        /// <returns>2D array of gray values</returns>
        public static byte[,] OpenImage(string ImagePath)
        {
            Bitmap original_bm = new Bitmap(ImagePath);
            int Height = original_bm.Height;
            int Width = original_bm.Width;

            byte[,] Buffer = new byte[Height, Width];

            unsafe
            {
                BitmapData bmd = original_bm.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, original_bm.PixelFormat);
                int x, y;
                int nWidth = 0;
                bool Format32 = false;
                bool Format24 = false;
                bool Format8 = false;

                if (original_bm.PixelFormat == PixelFormat.Format24bppRgb)
                {
                    Format24 = true;
                    nWidth = Width * 3;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format32bppArgb || original_bm.PixelFormat == PixelFormat.Format32bppRgb || original_bm.PixelFormat == PixelFormat.Format32bppPArgb)
                {
                    Format32 = true;
                    nWidth = Width * 4;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    Format8 = true;
                    nWidth = Width;
                }
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (y = 0; y < Height; y++)
                {
                    for (x = 0; x < Width; x++)
                    {
                        if (Format8)
                        {
                            Buffer[y, x] = p[0];
                            p++;
                        }
                        else
                        {
                            Buffer[y, x] = (byte)((int)(p[0] + p[1] + p[2]) / 3);
                            if (Format24) p += 3;
                            else if (Format32) p += 4;
                        }
                    }
                    p += nOffset;
                }
                original_bm.UnlockBits(bmd);
            }

            return Buffer;
        }

        /// <summary>
        /// Get the height of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Height</returns>
        public static int GetHeight(byte[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(0);
        }

        /// <summary>
        /// Get the width of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Width</returns>
        public static int GetWidth(byte[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(1);
        }

        /// <summary>
        /// Display the given image on the given PictureBox object
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <param name="PicBox">PictureBox object to display the image on it</param>
        public static void DisplayImage(byte[,] ImageMatrix, PictureBox PicBox)
        {
            // Create Image:
            //==============
            int Height = ImageMatrix.GetLength(0);
            int Width = ImageMatrix.GetLength(1);

            Bitmap ImageBMP = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

            unsafe
            {
                BitmapData bmd = ImageBMP.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, ImageBMP.PixelFormat);
                int nWidth = 0;
                nWidth = Width * 3;
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (int i = 0; i < Height; i++)
                {
                    for (int j = 0; j < Width; j++)
                    {
                        p[0] = p[1] = p[2] = ImageMatrix[i, j];
                        p += 3;
                    }

                    p += nOffset;
                }
                ImageBMP.UnlockBits(bmd);
            }
            PicBox.Image = ImageBMP;
        }

        public static void FixImage(byte[,] ImageMatrix, int Method)
        {
            switch (Method)
            {
                case 1:
                    AlphaTrim(ImageMatrix);
                    break;
                case 2:
                    MedianFilter(ImageMatrix);
                    break;
                default:
                    break;
            }
        }

        private static void AlphaTrim(byte[,] ImageMatrix)
        {
            const int T = 3;
            const int windowSize = 2; // 1 -> 3x3, 2-> 5x5, 3-> 7x7
            const bool evenOdd = false;

            for (int i = 0; i < ImageMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < ImageMatrix.GetLength(1); j++)
                {
                    List<byte> neighbours = new List<byte>();
                    int neighboursSum = 0;
                    for (int ineighbours = -1 * windowSize; ineighbours <= windowSize; ++ineighbours)
                    {
                        for (int jneighbours = -1 * windowSize; jneighbours <= windowSize; ++jneighbours)
                        {
                            if (ineighbours == jneighbours && ineighbours == 0)
                                continue;
                            if (ineighbours + i < 0 || jneighbours + j < 0)
                                continue;
                            if (ineighbours + i >= ImageMatrix.GetLength(0) || jneighbours + j >= ImageMatrix.GetLength(1))
                                continue;
                            neighbours.Add(ImageMatrix[ineighbours + i, jneighbours + j]);
                            // neighboursSum += ImageMatrix[ineighbours + i, jneighbours + j]; // might be inefficient
                        }
                    }
                    neighbours.Sort();
                    if (T * 2 < neighbours.Count)
                    {
                        if (neighbours.Count % 2 == 0 && evenOdd)
                        {
                             continue;
                        }
                        neighbours.RemoveRange(neighbours.Count - T, T);
                        neighbours.RemoveRange(0, T);
                        neighboursSum = 0;
                        foreach (var item in neighbours)
                        {
                            neighboursSum += item;
                        }
                        ImageMatrix[i, j] = (byte)(neighboursSum / neighbours.Count);
                    }
                }
            }
        }

        private static void MedianFilter(byte[,] ImageMatrix, int windowSize = 3) // 1 -> 3x3, 2-> 5x5, 3-> 7x7
        {
            if (windowSize >= Math.Min(ImageMatrix.GetLength(0), ImageMatrix.GetLength(1)))
            {
                return;
            }

            for (int i = 0; i < ImageMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < ImageMatrix.GetLength(1); j++)
                {
                    for (int k = 1; k <= windowSize; k++)
                    {
                        List<byte> neighbours = new List<byte>();
                        for (int ni = -1 * k; ni <= k; ++ni)
                        {
                            for (int nj = -1 * k; nj <= k; ++nj)
                            {
                                if (ni == nj && ni == 0)
                                    continue;
                                if (ni + i < 0 || nj + j < 0)
                                    continue;
                                if (ni + i >= ImageMatrix.GetLength(0) || nj + j >= ImageMatrix.GetLength(1))
                                    continue;
                                neighbours.Add(ImageMatrix[ni + i, nj + j]);
                            }
                        }
                        neighbours.Sort();
                        byte current = ImageMatrix[i, j];
                        byte max = neighbours[neighbours.Count - 1];
                        byte min = neighbours[0];
                        byte med = neighbours[neighbours.Count / 2];
                        if (neighbours.Count % 2 == 0)
                        {
                            med = (byte)((med + neighbours[(neighbours.Count / 2) + 1]) / 2);
                        }
                        byte A1 = (byte)((int)med - (int)min);
                        byte A2 = (byte)((int)max - (int)med);
                        if (A1 <= 0 || A2 <= 0)
                        {
                            if (k == windowSize)
                            {
                                ImageMatrix[i, j] = med;
                            }
                            continue;
                        }

                        byte B1 = (byte)((int)current - (int)min);
                        byte B2 = (byte)((int)max - (int)current);
                        ImageMatrix[i, j] = (B1 > 0 && B2 > 0) ? current : med;
                    }
                }
            }
        }
    }
}
