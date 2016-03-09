using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AForge.Math;
using System.Windows.Markup;
using System.Windows.Media;

namespace AudioToColorGUI {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private AudioAnalyzer AA = new AudioAnalyzer();

        public MainWindow() {
            InitializeComponent();
        }

        private void DropZone_Drop(object sender, DragEventArgs e) {

            String[] droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            int k = 0;
            int height = 100;
            int width = 14;
            int yp = 0;
            int margin = 30;
            foreach (var s in droppedFilePaths) {
                Color[] colors = AA.AnalyzeAudio(s);
                for(int i = 0; i < colors.Length; i++) {
                    //Console.WriteLine(colors[i].R + "," + colors[i].G + "," + colors[i].B);
                    Rectangle rect = new Rectangle();
                    rect.Fill = new SolidColorBrush(colors[i]);
                    rect.Width = width;
                    rect.Height = height;
                    MainCanvas.Children.Add(rect);
                    Canvas.SetLeft(rect, i*width);
                    Canvas.SetTop(rect, yp);
                }
                k++;

                TextBlock title = new TextBlock();
                MainCanvas.Children.Add(title);
                Canvas.SetLeft(title, width);
                Canvas.SetTop(title, yp+height);
                string fileName = "";
                for(int i = s.Length-1; i >= 0; i--) {
                    if (Convert.ToString(s[i]) == @"\") {
                        fileName = s.Substring(i+1, s.Length -1 -i);
                        break;
                    }
                }
                title.Text = fileName;
                yp = yp + height + margin;
            }

            /*
            int[] tempos = { 120, 133, 132, 102, 127, 172, 94, 90, 85, 108, 90, 125, 87, 107, 128 };
            int[] normalizedTempos = AA.NormalizeTempo(tempos);
            for(int i = 0; i < tempos.Length; i++) {
                Console.WriteLine(tempos[i] + ":" + normalizedTempos[i]);
            }
            */

            WindowState = WindowState.Maximized;
            //DropZone.Visibility = Visibility.Hidden;
        }
    }

    class AudioAnalyzer {

        public static int numberOfSamples = 130;

        public Color[] AnalyzeAudio(String path) {

            int hue = 200;

            //string path = @"\Users\Nova\Documents\ColorProject\Drop.wav";
            Song song = new Song(path, numberOfSamples);
            return getColorArray(hue, NormalizeAmplitude(song.complexDataShortened), NormalizeFrequnecy(song.frequneciesShortened));
        }

        public Color[] getColorArray(int hue, int[] amp, int[] frq) {
            Color[] colorArray = new Color[numberOfSamples];
            for(int i = 0; i < numberOfSamples; i++) {
                colorArray[i] = HSVToRGB(hue, frq[i], amp[i]); // hue, saturation, value
            }
            return colorArray;
        }

        /*
        public int[] NormalizeTempo(int[] complex) {
            int[] newArray = new int[complex.Length];
            double maxValue = 360;
            double minValue = 0;
            double foundMaxValue = 0;
            double foundMinValue = 100;

            for(int i =0; i<complex.Length;i++) {
                if (complex[i] > foundMaxValue) {
                    foundMaxValue = complex[i];
                }
                if (complex[i] < foundMinValue) {
                    foundMinValue = complex[i];
                }
            }
            for (int i = 0; i < complex.Length; i++) {
                int val = ScaleDataRange(foundMinValue, foundMaxValue, minValue, maxValue, complex[i]);
                newArray[i] = val;
                //Console.WriteLine(" " + complex[i].Re);
                //Console.WriteLine("new = " + newArray[i]);
            }
            return newArray;
        }
        */

        public int[] NormalizeAmplitude(Complex[] complex) {

            double ampFloor = 0.9;
            int[] newArray = new int[complex.Length];
            double maxValue = 100;
            double minValue = 0;
            double foundMaxValue = 0;
            double foundMinValue = 100;

            for (int i = 0; i < complex.Length; i++) {
                if(complex[i].Re < ampFloor) {
                    complex[i].Re = ampFloor;
                }
                if (complex[i].Re > foundMaxValue) {
                    foundMaxValue = complex[i].Re;
                }
                if(complex[i].Re < foundMinValue) {
                    foundMinValue = complex[i].Re;
                }
            }
            for (int i = 0; i < complex.Length; i++) {
                int val = ScaleDataRange(foundMinValue, foundMaxValue, minValue, maxValue, complex[i].Re);
                newArray[i] = val;
                //Console.WriteLine("new = " + newArray[i]);
            }
            return newArray;
        }

        public int[] NormalizeFrequnecy(Complex[] complex) {
            int[] newArray = new int[complex.Length];
            double maxValue = 100;
            double minValue = 0;
            double foundMaxValue = 0;
            double foundMinValue = 100;

            foreach (var v in complex) {
                if (v.Re > foundMaxValue) {
                    foundMaxValue = v.Re;
                }
                if (v.Re < foundMinValue) {
                    foundMinValue = v.Re;
                }
            }
            for (int i = 0; i < complex.Length; i++) {
                int val = ScaleDataRange(foundMinValue, foundMaxValue, minValue, maxValue, complex[i].Re);
                newArray[i] = val;
                //Console.WriteLine(" " + complex[i].Re);
                //Console.WriteLine("new = " + newArray[i]);
            }
            return newArray;
        }

        public int ScaleDataRange(double originalStart, double originalEnd, double newStart, double newEnd, double value) {
            double scale = (newEnd - newStart) / (originalEnd - originalStart);
            return (int)(newStart + ((value - originalStart) * scale));
        }

        public Color HSVToRGB(double hue, double saturation, double value) {
            WpfExtensions.HslColorExtension hc = new WpfExtensions.HslColorExtension();
            hc.H = hue;
            hc.S = saturation;
            hc.L = value;
            hc.A = 100;
            return hc.GetColor();
        }
    }

    class Song {

        //(int)Math.Pow(2, Math.Floor(Math.Log(complexData.Length) / Math.Log(2)));

        public string path = null;
        public byte[] wavData;
        public Complex[] complexData;
        public Complex[] complexDataShortened;
        public Complex[] frequencies;
        public Complex[] frequneciesShortened;

        //File Head Data
        public byte[] chunkIDField = new byte[4];
        public byte[] formatField = new byte[4];
        public byte[] numberOfChannelsField = new byte[2];
        public byte[] sampleRateField = new byte[4];
        public byte[] byteRateField = new byte[4];
        public byte[] bitsPerSampleField = new byte[2];

        //Parsed File Head Data
        public int numberOfChannels = 0;
        public double sampleRate = 0;
        public double byteRate = 0;
        public double bitsPerSample = 0;

        //FormatChecks
        public byte[] RIFF = { 82, 73, 70, 70 }; // = RIFF
        public byte[] Format = { 87, 65, 86, 69 }; // = WAVE

        private int numberOfSamples;
        private int SamplesPerFrq = 8192; //16384;
        private int numberOfFrequnecies;

        private int frequnecyCutOff = 500;

        public Song(string path, int numSamples) {

            this.path = path;
            wavData = File.ReadAllBytes(path);

            numberOfSamples = numSamples;

            ReadHeader();
            ConvertToNormalizedMono();

            complexDataShortened = ShortenArray(complexData, numberOfSamples);

            GetFrequencies(complexData);
            frequneciesShortened = ShortenArray(frequencies, numberOfSamples);

            //PrintFrequencies();

            //Console.WriteLine(wavData.Length);
        }

        public void ReadHeader() {

            Array.Copy(wavData, 0, chunkIDField, 0, 4);
            Array.Copy(wavData, 8, formatField, 0, 4);
            Array.Copy(wavData, 22, numberOfChannelsField, 0, 2);
            Array.Copy(wavData, 24, sampleRateField, 0, 4);
            Array.Copy(wavData, 28, byteRateField, 0, 4);
            Array.Copy(wavData, 34, bitsPerSampleField, 0, 2);

            StringBuilder sr = new StringBuilder("");
            StringBuilder br = new StringBuilder("");
            for (int i = 3; i >= 0; i--) {
                sr.Append(Convert.ToString(sampleRateField[i], 2).PadLeft(8, '0'));
                br.Append(Convert.ToString(byteRateField[i], 2).PadLeft(8, '0'));
            }

            StringBuilder nc = new StringBuilder("");
            StringBuilder bs = new StringBuilder("");
            for (int i = 1; i >= 0; i--) {
                nc.Append(Convert.ToString(numberOfChannelsField[i], 2).PadLeft(8, '0'));
                bs.Append(Convert.ToString(bitsPerSampleField[i], 2).PadLeft(8, '0'));
            }

            numberOfChannels = Convert.ToInt32(nc.ToString(), 2);
            sampleRate = Convert.ToInt32(sr.ToString(), 2);
            byteRate = Convert.ToInt32(br.ToString(), 2);
            bitsPerSample = Convert.ToInt32(bs.ToString(), 2);

            Console.WriteLine("Sample Rate = " + sampleRate);
            Console.WriteLine("number of channels = " + numberOfChannels);
            Console.WriteLine("byte rate = " + byteRate);
            Console.WriteLine("bits per sample = " + bitsPerSample);

            if (!chunkIDField.SequenceEqual(RIFF)) {
                Console.WriteLine("File not of type RIFF");
            }
            if (!formatField.SequenceEqual(Format)) {
                Console.WriteLine("File is not a .wav");
            }
            if (bitsPerSample != 16) {
                Console.WriteLine("File has " + bitsPerSample + " bits per sample");
            }
        }

        public void ConvertToNormalizedMono() {
            double complexLength = wavData.Length / 4;
            complexData = new Complex[(int)Math.Ceiling(complexLength)];
            int j = 0;
            for (int i = 44; i < wavData.Length; i += 4) {
                double combinedLeft = wavData[i + 1] << 8 | wavData[i];
                double combinedRight = wavData[i + 3] << 8 | wavData[i + 2];
                double mono = (combinedLeft + combinedRight) / 2;
                complexData[j] = new Complex((mono / 32760), 0);
                j += 1;
            }
        }

        public Complex[] ShortenArray(Complex[] source, int size) {

            Complex[] shortened = new Complex[size];

            double ratio = (double)source.Length / (double)size;
            int roundedRatio = (int)Math.Ceiling(ratio);
            int k = 0;
            for (int i = 0; i < source.Length; i += roundedRatio) {
                double sum = 0;
                for (int j = 0; j < roundedRatio; j++) {
                    if (i + j >= source.Length) {
                        break;
                    } else {
                        sum += source[i + j].Re;
                    }
                }
                double avg = sum / roundedRatio;
                shortened[k] = new Complex(avg, 0);
                k++;
            }

            return shortened;
        }
        public void GetFrequencies(Complex[] source) {
            int numberOfFrequnecies = (int)(source.Length / SamplesPerFrq);
            frequencies = new Complex[numberOfFrequnecies];
            Complex[] frequnecySamples = new Complex[SamplesPerFrq];

            int n = 0;
            for(int i = 0; i < source.Length - SamplesPerFrq; i += SamplesPerFrq) {
                int k = 0;
                for (int j = i; j < SamplesPerFrq+i; j++) {
                    frequnecySamples[k] = source[j];
                    k++;
                }
                FourierTransform.FFT(frequnecySamples, FourierTransform.Direction.Forward);
                double max = 0;
                int maxIndex = 0;
                for(int m = 1; m < frequnecySamples.Length; m++) {
                    if (frequnecySamples[m].Re > max) {
                        max = frequnecySamples[m].Re;
                        maxIndex = m;
                    }
                }
                double fq = maxIndex * sampleRate / SamplesPerFrq;
                if(fq > frequnecyCutOff) {
                    fq = frequnecyCutOff;
                }
                frequencies[n].Re = fq;
                n++;
            }
        }

        public void GetFrequenciesOld() {
            int FFTLength = complexDataShortened.Length; //16384 max
            frequencies = new Complex[FFTLength];
            Array.Copy(complexDataShortened, frequencies, FFTLength);
            FourierTransform.FFT(frequencies, FourierTransform.Direction.Forward);
            frequencies[0] = new Complex(0, 0);


        }

        public void PrintFrequencies() {
            //StringBuilder sb = new StringBuilder("");
            double maxfrq = -10000;
            double minfrq = 10000;
            int over2000 = 0;
            int over4000 = 0;
            int over6000 = 0;
            int over8000 = 0;
            foreach (var v in frequencies) {
                if (v.Re > maxfrq) {
                    maxfrq = v.Re;
                }
                if (v.Re < minfrq) {
                    minfrq = v.Re;
                }
                if(v.Re > 250) {
                    over2000++;
                    if(v.Re > 500) {
                        over4000++;
                        if(v.Re > 1000) {
                            over6000++;
                            if (v.Re > 2000) {
                                over8000++;

                            }
                        }
                    }
                }
                //Console.WriteLine(v.Re);
            }
            Console.WriteLine("max = "+maxfrq);
            Console.WriteLine("min = " + minfrq);
            Console.WriteLine("250 = " + over2000);
            Console.WriteLine("500 = " + over4000);
            Console.WriteLine("1000 = " + over6000);
            Console.WriteLine("2000 = " + over8000);
        }
    }
}
