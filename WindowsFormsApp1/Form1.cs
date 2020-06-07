using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using NeuralNetworks.Library;
using NeuralNetworks.Library.Components.Activation;
using NeuralNetworks.Library.Training;
using NeuralNetworks.Library.Training.BackPropagation;
using NeuralNetworks.Library.Extensions;
using NeuralNetworks.Library.Data;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private const string BASEFOLDER = @"./";

        private bool m_IsDown = false;
        private int[] m_vector = new int[64];

        private NeuralNetwork neuralNetwork;

        public Form1()
        {
            InitializeComponent();
        }

        private void Panel1_MouseDown(object sender, MouseEventArgs e)
        {
            m_IsDown = true;
        }

        private void Panel1_MouseUp(object sender, MouseEventArgs e)
        {
            m_IsDown = false;
        }

        private void Panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (m_IsDown == false)
                return;

            int x = e.X;
            int y = e.Y;

            int w = panel1.Width / 8;
            int h = panel1.Height / 8;

            int i = y / h;
            int j = x / w;

            int index = 8 * i + j;
            m_vector[index] = 1;

            panel1.Invalidate(true);
        }

        private void Panel1_Paint(object sender, PaintEventArgs e)
        {
            for (int index = 0; index < 64; index++)
            {
                if (m_vector[index] == 1)
                {
                    int i = index / 8;
                    int j = index % 8;

                    int w = panel1.Width / 8;
                    int h = panel1.Height / 8;

                    int x = j * w;
                    int y = i * h;

                    e.Graphics.FillRectangle(Brushes.White, x, y, w, h);
                }
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            string letter;
            letter = textBox1.Text.Trim().ToUpper();

            if(letter != "A" && letter != "B" && letter != "C")
            {
                MessageBox.Show("La classe doit être A, B ou C");
                return;
            }

            string s = "";
            for (int index = 0; index < 64; index++)
            {
                s = s + m_vector[index].ToString();
            }

            string filename = BASEFOLDER + "Base.txt";
            File.AppendAllText(filename, s + "\n");
            File.AppendAllText(filename, letter + "\n");
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            progressBar1.Value = 20;
            neuralNetwork = NeuralNetwork.For(NeuralNetworkContext.MaximumPrecision)
                .WithInputLayer(neuronCount: 64, activationType: ActivationType.Sigmoid)
                .WithHiddenLayer(neuronCount: 10, activationType: ActivationType.Sigmoid)
                .WithOutputLayer(neuronCount: 3, activationType: ActivationType.Sigmoid)
                .Build();
            progressBar1.Value = 40;
            await TrainingController
                .For(BackPropagation.WithConfiguration(
                        neuralNetwork,
                        ParallelOptionsExtensions.UnrestrictedMultiThreadedOptions,
                        learningRate: 1.8,
                        momentum: 0.9))
                .TrainForEpochsOrErrorThresholdMet(GetMyTrainingData(), maximumEpochs: 5000, errorThreshold: 0.05);
            progressBar1.Value = 100;
            button4.Visible = true;
            progressBar1.Visible=false;    
        }

        private static List<TrainingDataSet> GetMyTrainingData()
        {
            // 2 tableaux
            // inputs : double[22][64]
            // outputs : double[22][3]

            string filename = BASEFOLDER + "Base.txt";

            string[] lines = File.ReadAllLines(filename);   // lines va contenir 44 lines

            int nbSamples = lines.Length / 2;   // 22 exemples

            double[][] inputs = new double[nbSamples][];
            double[][] outputs = new double[nbSamples][];

            for (int i = 0; i<nbSamples; i++)
            {
                inputs[i] = new double[64];     // inputs[i] est de type double[]
                outputs[i] = new double[3];

                // lines[2*i+0] contient la string "0000010101001001001010101010"
                // lines[2*i+1] contient la string "C"

                int k = 0;
                foreach(char c in lines[2*i])
                {
                    if (c == '0')
                        inputs[i][k] = 0.0;

                    if (c == '1')
                        inputs[i][k] = 1.0;

                    k++;
                }

                if (lines[2 * i + 1] == "A")
                    { outputs[i][0] = 1.0; outputs[i][1] = 0.0; outputs[i][2] = 0.0; }
                if (lines[2 * i + 1] == "B")
                    { outputs[i][0] = 0.0; outputs[i][1] = 1.0; outputs[i][2] = 0.0; }
                if (lines[2 * i + 1] == "C")
                    { outputs[i][0] = 0.0; outputs[i][1] = 0.0; outputs[i][2] = 1.0; }

            }

            return inputs.Select((input, i) => TrainingDataSet.For(input, outputs[i])).ToList();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 64; i++)
            {
                m_vector[i] = 0;
            }

            panel1.Invalidate(true);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            double[] input = new double[64];

            for (int i = 0; i < m_vector.Length; i++)
                input[i] = m_vector[i];

            var output = neuralNetwork.PredictionFor(input, ParallelOptionsExtensions.SingleThreadedOptions);

            // string s = output[0] + " " + output[1] + " " + output[2];

            double maxValue = output.Max();
            int maxIndex = output.ToList().IndexOf(maxValue);

            if(maxIndex == 0)
                MessageBox.Show("A");
            if (maxIndex == 1)
                MessageBox.Show("B");
            if (maxIndex == 2)
                MessageBox.Show("C");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button4.Visible =false;
        }
    }
 }
