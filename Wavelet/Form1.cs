using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Wavelet
{
    public partial class Form1 : Form
    {
        private BitReader bitReader;
        private string input;
        private string inputWavelet;
        private int imgSize;
        private byte[] antet;
        private double[,] mOriginala;
        private double[,] mPrelucrata;
        private double[] analysisL = { 0.026748757411, -0.016864118443, -0.078223266529, 0.266864118443, 0.602949018236, 0.266864118443, -0.078223266529, -0.016864118443, 0.026748757411 };
        private double[] analysisH = { 0.000000000000, 0.091271763114, -0.057543526229, -0.591271763114, 1.115087052457, -0.591271763114, -0.057543526229, 0.091271763114, 0.000000000000 };
        private double[] synthesisL = { 0.000000000000, -0.091271763114, -0.057543526229, 0.591271763114, 1.115087052457, 0.591271763114, -0.057543526229, -0.091271763114, 0.000000000000 };
        private double[] synthesisH = { 0.026748757411, 0.016864118443, -0.078223266529, -0.266864118443, 0.602949018236, -0.266864118443, -0.078223266529, 0.016864118443, 0.026748757411 };
        public Form1()
        {
            InitializeComponent();
            input = null;
            inputWavelet = null;
            imgSize = 512;
            antet = new byte[1078];
            mOriginala = new double[imgSize, imgSize];
            mPrelucrata = new double[imgSize, imgSize];
        }

        private double[,] CitireImagine(double[,] mImagine)
        {
            bitReader = new BitReader(input);

            for (int i = 0; i < 1078; i++)
            {
                antet[i] = (byte)bitReader.ReadNBits(8);
            }

            for (int i = 0; i < imgSize; i++)
            {
                for (int j = 0; j < imgSize; j++)
                {
                    int pixel = bitReader.ReadNBits(8);
                    mImagine[i, j] = (double)pixel;
                    mOriginala[i, j] = (double)pixel;
                }
            }

            return mImagine;
        }

        private double[,] CitireImagine(double[,] mImagine, string input)
        {
            FileStream fileStream = new FileStream(input, FileMode.Open);
            using (BinaryReader binaryReader = new BinaryReader(fileStream))
            {
                for (int i = 0; i < imgSize; i++)
                {
                    for (int j = 0; j < imgSize; j++)
                    {
                        double pixel = Normalizare(binaryReader.ReadDouble());
                        mPrelucrata[i, j] = pixel;
                    }
                }
            }

            return mImagine;
        }

        private void loadBtn_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = null;
            pictureBox2.Image = null;

            openFileDialog1.Filter = "bmp files (*.bmp)|*.bmp|All files (*.*)|*.*";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                input = openFileDialog1.FileName;
            }

            FileStream fs = new System.IO.FileStream(input, FileMode.Open, FileAccess.Read);
            pictureBox1.Image = Image.FromStream(fs);
            fs.Close();

            mPrelucrata = CitireImagine(mPrelucrata);
        }

        private double[] ConstruireLinieCuReflexie(double[,] matrice, int indexLinie, int length, double[] linieCuReflexie, string option)
        {
            if (option == "horizontal")
            {
                for (int i = 0; i < length; i++)
                {
                    linieCuReflexie[i + 4] = matrice[i, indexLinie];
                }
            }
            else if(option == "vertical")
            {
                for (int i = 0; i < length; i++)
                {
                    linieCuReflexie[i + 4] = matrice[indexLinie, i];
                }
            }

            for (int i = 0; i < 4; i++)
            {
                linieCuReflexie[i] = linieCuReflexie[8 - i];
                linieCuReflexie[length + 4 + i] = linieCuReflexie[length + 2 - i];
            }

            return linieCuReflexie;
        }

        private void AplicareFiltru(ref double[] filtruLow, ref double[] filtruHigh, double[] filtruL, double[] filtruH, double[] linieCuReflexie, int length)
        {
            int offset = 4;
            int sizeWithOffset = length + offset;

            for (int i = offset; i < sizeWithOffset; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    filtruLow[i - 4] += filtruL[j] * linieCuReflexie[i - (j - 4)];
                    filtruHigh[i - 4] += filtruH[j] * linieCuReflexie[i - (j - 4)];
                }

            }
        }

        private double[] Downsampling(int length, ref double[] analysisLow, ref double[] analysisHigh, ref double[] downSampleLow, ref double[] downSampleHigh, ref double[] rearrange)
        {
            for (int i = 0; i < length; i++)
            {
                if (i % 2 == 0)
                {
                    downSampleLow[i] = analysisLow[i];
                }
                else
                {
                    downSampleHigh[i] = analysisHigh[i];
                }
            }

            for (int i = 0; i < length/2; i++)
            {
                rearrange[i] = downSampleLow[i * 2];
            }

            for (int i = length / 2, j = 0; i < length && j < length / 2; i++, j++)
            {
                rearrange[i] = downSampleHigh[j * 2 + 1];
            }

            return rearrange;
        }

        private void Analysis(int indexLinie, int length, string option)
        {
            double[] analysisLow = new double[length];
            double[] analysisHigh = new double[length];
            double[] linieCuReflexie = new double[length + 8];
            double[] downSampleLow = new double[length];
            double[] downSampleHigh = new double[length];
            double[] rearrange = new double[length];

            linieCuReflexie = ConstruireLinieCuReflexie(mPrelucrata, indexLinie, length, linieCuReflexie, option);

            AplicareFiltru(ref analysisLow, ref analysisHigh, analysisL, analysisH, linieCuReflexie, length);

            rearrange = Downsampling(length, ref analysisLow, ref analysisHigh, ref downSampleLow, ref downSampleHigh, ref rearrange);

            if (option == "horizontal")
            {
                for (int i = 0; i < length; i++)
                {
                    mPrelucrata[i, indexLinie] = rearrange[i];
                }
            }

            else if (option == "vertical")
            {
                for (int i = 0; i < length; i++)
                {
                    mPrelucrata[indexLinie, i] = rearrange[i];
                }
            }

        }

        private double[] ConstruireLinieCuReflexie(double[] linie, int indexLinie, int length, double[] linieCuReflexie)
        {

            for (int i = 0; i < length; i++)
            {
                linieCuReflexie[i + 4] = linie[i];
            }

            for (int i = 0; i < 4; i++)
            {
                linieCuReflexie[i] = linieCuReflexie[8 - i];
                linieCuReflexie[length + 4 + i] = linieCuReflexie[length + 2 - i];
            }

            return linieCuReflexie;
        }

        private void AplicareFiltru(ref double[] filtruLow, ref double[] filtruHigh, double[] filtruL, double[] filtruH, double[] extendedLow, double[] extendedHigh, int length)
        {
            int offset = 4;
            int sizeWithOffset = length + offset;

            for (int i = offset; i < sizeWithOffset; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    filtruLow[i - 4] += filtruL[j] * extendedLow[i - (j - 4)];
                    filtruHigh[i - 4] += filtruH[j] * extendedHigh[i - (j - 4)];
                }

            }
        }

        private void Synthesis(int indexLinie, int length, string option)
        {
            double[] sampleLow = new double[length];
            double[] sampleHigh = new double[length];
            double[] sampleLowExtended = new double[length + 8];
            double[] sampleHighExtended = new double[length + 8];
            double[] synthesisLow = new double[length];
            double[] synthesisHigh = new double[length];
            double[] reconstructedLine = new double[length];

            if (option == "horizontal")
            {
                for (int i = 0; i < length / 2; i++)
                {
                    sampleLow[i * 2] = mPrelucrata[i, indexLinie];
                }

                for (int i = length / 2, j = 0; i < length && j < length / 2; i++, j++)
                {
                    sampleHigh[j * 2 + 1] = mPrelucrata[i, indexLinie];
                }
            }

            else if (option == "vertical")
            {
                for (int i = 0; i < length / 2; i++)
                {
                    sampleLow[i * 2] = mPrelucrata[indexLinie, i];
                }

                for (int i = length / 2, j = 0; i < length && j < length / 2; i++, j++)
                {
                    sampleHigh[j * 2 + 1] = mPrelucrata[indexLinie, i];
                }
            }

            sampleLowExtended = ConstruireLinieCuReflexie(sampleLow, indexLinie, length, sampleLowExtended);
            sampleHighExtended = ConstruireLinieCuReflexie(sampleHigh, indexLinie, length, sampleHighExtended);

            AplicareFiltru(ref synthesisLow, ref synthesisHigh, synthesisL, synthesisH, sampleLowExtended, sampleHighExtended, length);

            if (option == "horizontal")
            {
                for (int i = 0; i < length; i++)
                {
                    if (length == imgSize)
                    {
                        reconstructedLine[i] = Math.Round(synthesisLow[i] + synthesisHigh[i]);
                    }
                    else
                    {
                        reconstructedLine[i] = synthesisLow[i] + synthesisHigh[i];
                    }

                    mPrelucrata[i, indexLinie] = reconstructedLine[i];
                }
            }

            else if (option == "vertical")
            {
                for (int i = 0; i < length; i++)
                {
                    if (length == imgSize)
                    {
                        reconstructedLine[i] = Math.Round(synthesisLow[i] + synthesisHigh[i]);
                    }
                    else
                    {
                        reconstructedLine[i] = synthesisLow[i] + synthesisHigh[i];
                    }

                    mPrelucrata[indexLinie, i] = reconstructedLine[i];

                }
            }

        }

        private void testErrorBtn_Click(object sender, EventArgs e)
        {
            double min = double.MaxValue;
            double max = double.MinValue;

            double pixel;

            for (int i = 0; i < imgSize; i++)
            {
                for (int j = 0; i < imgSize; i++)
                {
                    pixel = mOriginala[i, j] - mPrelucrata[i, j];

                    if(pixel < min)
                    {
                        min = pixel;
                    }
                    else if(pixel > max)
                    {
                        max = pixel;
                    }
                }
            }

            minTextBox.Text = min.ToString();
            maxTextBox.Text = max.ToString();
        }

        private void anH1Btn_Click(object sender, EventArgs e)
        {
            xNumericUpDown.Value = imgSize / 2; 

            for (int i = 0; i < imgSize; i++)
            {
                Analysis(i, imgSize, "horizontal");
            }
        }

        private void anV1Btn_Click(object sender, EventArgs e)
        {
            yNumericUpDown.Value = imgSize / 2;

            for (int i = 0; i < imgSize; i++)
            {
                Analysis(i, imgSize, "vertical");
            }
        }

        private void anH2Btn_Click(object sender, EventArgs e)
        {
            xNumericUpDown.Value = imgSize / 4;

            for (int i = 0; i < imgSize/2; i++)
            {
                Analysis(i, imgSize / 2, "horizontal");
            }
        }

        private void anV2Btn_Click(object sender, EventArgs e)
        {
            yNumericUpDown.Value = imgSize / 4;

            for (int i = 0; i < imgSize/2; i++)
            {
                Analysis(i, imgSize / 2, "vertical");
            }
        }

        private void anH3Btn_Click(object sender, EventArgs e)
        {
            xNumericUpDown.Value = imgSize / 8;

            for (int i = 0; i < imgSize/4; i++)
            {
                Analysis(i, imgSize / 4, "horizontal");
            }
        }

        private void anV3Btn_Click(object sender, EventArgs e)
        {
            yNumericUpDown.Value = imgSize / 8;

            for (int i = 0; i < imgSize/4; i++)
            {
                Analysis(i, imgSize / 4, "vertical");
            }
        }

        private void anH4Btn_Click(object sender, EventArgs e)
        {
            xNumericUpDown.Value = imgSize / 16;

            for (int i = 0; i < imgSize/8; i++)
            {
                Analysis(i, imgSize / 8, "horizontal");
            }
        }

        private void anV4Btn_Click(object sender, EventArgs e)
        {
            yNumericUpDown.Value = imgSize / 16;

            for (int i = 0; i < imgSize/8; i++)
            {
                Analysis(i, imgSize / 8, "vertical");
            }
        }

        private void anH5Btn_Click(object sender, EventArgs e)
        {
            xNumericUpDown.Value = imgSize / 32;

            for (int i = 0; i < imgSize/16; i++)
            {
                Analysis(i, imgSize / 16, "horizontal");
            }
        }

        private void anV5Btn_Click(object sender, EventArgs e)
        {
            yNumericUpDown.Value = imgSize / 32;

            for (int i = 0; i < imgSize / 16; i++)
            {
                Analysis(i, imgSize / 16, "vertical");
            }
        }

        private void anAllBtn_Click(object sender, EventArgs e)
        {
            anH1Btn_Click(sender, e);
            anV1Btn_Click(sender, e);
            anH2Btn_Click(sender, e);
            anV2Btn_Click(sender, e);
            anH3Btn_Click(sender, e);
            anV3Btn_Click(sender, e);
            anH4Btn_Click(sender, e);
            anV4Btn_Click(sender, e);
            anH5Btn_Click(sender, e);
            anV5Btn_Click(sender, e);
        }

        private void synH1Btn_Click(object sender, EventArgs e)
        {
            xNumericUpDown.Value = imgSize;
            yNumericUpDown.Value = imgSize;

            for (int i = 0; i < imgSize; i++)
            {
                Synthesis(i, imgSize, "horizontal");
            }
        }

        private void synV1Btn_Click(object sender, EventArgs e)
        {
            xNumericUpDown.Value = imgSize;
            yNumericUpDown.Value = imgSize;

            for (int i = 0; i < imgSize; i++)
            {
                Synthesis(i, imgSize, "vertical");
            }
        }

        private void synH2Btn_Click(object sender, EventArgs e)
        {
            xNumericUpDown.Value = imgSize / 2;
            yNumericUpDown.Value = imgSize / 2;

            for (int i = 0; i < imgSize/2; i++)
            {
                Synthesis(i, imgSize / 2, "horizontal");
            }
        }

        private void synV2Btn_Click(object sender, EventArgs e)
        {
            xNumericUpDown.Value = imgSize / 2;
            yNumericUpDown.Value = imgSize / 2;

            for (int i = 0; i < imgSize/2; i++)
            {
                Synthesis(i, imgSize/2, "vertical");
            }
        }

        private void synH3Btn_Click(object sender, EventArgs e)
        {
            xNumericUpDown.Value = imgSize / 4;
            yNumericUpDown.Value = imgSize / 4;

            for (int i = 0; i < imgSize/4; i++)
            {
                Synthesis(i, imgSize / 4, "horizontal");
            }
        }

        private void synV3Btn_Click(object sender, EventArgs e)
        {
            xNumericUpDown.Value = imgSize / 4;
            yNumericUpDown.Value = imgSize / 4;

            for (int i = 0; i < imgSize / 4; i++)
            {
                Synthesis(i, imgSize / 4, "vertical");
            }
        }

        private void synH4Btn_Click(object sender, EventArgs e)
        {
            xNumericUpDown.Value = imgSize / 8;
            yNumericUpDown.Value = imgSize / 8;

            for (int i = 0; i < imgSize / 8; i++)
            {
                Synthesis(i, imgSize / 8, "horizontal");
            }
        }

        private void synV4Btn_Click(object sender, EventArgs e)
        {
            xNumericUpDown.Value = imgSize / 8;
            yNumericUpDown.Value = imgSize / 8;

            for (int i = 0; i < imgSize / 8; i++)
            {
                Synthesis(i, imgSize / 8, "vertical");
            }
        }

        private void synH5Btn_Click(object sender, EventArgs e)
        {
            xNumericUpDown.Value = imgSize / 16;
            yNumericUpDown.Value = imgSize / 16;

            for (int i = 0; i < imgSize / 16; i++)
            {
                Synthesis(i, imgSize / 16, "horizontal");
            }
        }

        private void synV5Btn_Click(object sender, EventArgs e)
        {
            xNumericUpDown.Value = imgSize / 16;
            yNumericUpDown.Value = imgSize / 16;

            for (int i = 0; i < imgSize / 16; i++)
            {
                Synthesis(i, imgSize / 16, "vertical");
            }
        }

        private void synAllBtn_Click(object sender, EventArgs e)
        {
            synV5Btn_Click(sender, e);
            synH5Btn_Click(sender, e);
            synV4Btn_Click(sender, e);
            synH4Btn_Click(sender, e);
            synV3Btn_Click(sender, e);
            synH3Btn_Click(sender, e);
            synV2Btn_Click(sender, e);
            synH2Btn_Click(sender, e);
            synV1Btn_Click(sender, e);
            synH1Btn_Click(sender, e);
        }

        private int Normalizare(int pixel)
        {
            if (pixel < 0)
            {
                return 0;
            }

            else if (pixel > 255)
            {
                return 255;
            }

            else
            {
                return pixel;
            }
        }

        private double Normalizare(double pixel)
        {
            if (pixel < 0)
            {
                return 0;
            }

            else if (pixel > 255)
            {
                return 255;
            }

            else
            {
                return pixel;
            }
        }

        private void visualizeBtn_Click(object sender, EventArgs e)
        {
            Bitmap bmp = new Bitmap(imgSize, imgSize);

            for (int i = 0; i < imgSize; i++)
            {
                for (int j = 0; j < imgSize; j++)
                {
                    if (i <= yNumericUpDown.Value && j <= xNumericUpDown.Value)
                    {
                        bmp.SetPixel(i, j, Color.FromArgb(Normalizare((int)mPrelucrata[i, j]), Normalizare((int)mPrelucrata[i, j]), Normalizare((int)mPrelucrata[i, j])));
                    }
                    else
                    {
                        int pixelScaling = (int)scaleNumericUpDown.Value * (int)Math.Round(mPrelucrata[i, j]) + (int)offsetNumericUpDown.Value;
                        pixelScaling = Normalizare(pixelScaling);
                        bmp.SetPixel(i, j, Color.FromArgb(pixelScaling, pixelScaling, pixelScaling));
                    }
                }
            }

            bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
            pictureBox2.Image = bmp;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void loadEncodedBtn_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = null;
            pictureBox2.Image = null;

            openFileDialog2.Filter = "wvt files (*.wvt)|*.wvt|All files (*.*)|*.*";
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                inputWavelet = openFileDialog2.FileName;
                
            }
            mPrelucrata = CitireImagine(mPrelucrata, inputWavelet);
        }

        private void saveBtn_Click(object sender, EventArgs e)
        {

            SaveFileDialog savefile = new SaveFileDialog();
            savefile.FileName = input + ".wvt";

            FileStream fileStream = new FileStream(savefile.FileName, FileMode.Create);
                using (BinaryWriter bw = new BinaryWriter(fileStream))
                {
                    for (int i = 0; i < imgSize; i++)
                    {
                        for (int j = 0; j < imgSize; j++)
                        {
                            bw.Write(mPrelucrata[i, j]);
                        }
                    }

                }

            fileStream.Close();

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }
    }
}

