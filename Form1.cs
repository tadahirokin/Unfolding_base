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
using System.Text.RegularExpressions;
using MathNet.Numerics.LinearAlgebra;
using Filtering;
using System.Windows.Forms.DataVisualization.Charting;



namespace Unfolding_base
{


    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            rm_filepath = "C:\\Users\\Tadahiro Kin\\OneDrive - Kyushu University\\Takahashi-kun\\共同研究報告書など\\K11413-A02_unfolding_sample_code\\src\\rmf.dat";
        }
        private int num_spe;
        private string spe_filepath;
        private string rm_filepath;
        private bool flg_spe, flg_response;
        //private double[,] responseMatrix; //[mono energy gamma ID, channel No.]
        //Matrix<double> res_mat;

        private int res_num;
        private int ene_num;

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();


            ofd.FileName = "NaI_data_.csv";
            ofd.Filter = "CSV file (*.csv)|*.csv|all file (*.*)|*.*";
            ofd.Title = "Open spectra file (Mitsubishi format)";
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                spe_filepath = ofd.FileName;

                string tmp = "Spectra file name is " + Path.GetFileName(spe_filepath) + Environment.NewLine;
                textBox1.AppendText(tmp);

                string[] all_data = File.ReadAllLines(spe_filepath);
                num_spe = all_data.Length - 1;

                tmp = "Number of. spectra: " + num_spe.ToString() + Environment.NewLine+ Environment.NewLine;

                textBox1.AppendText(tmp);

                textBox7.Text = spe_filepath + "_out.txt";



                flg_spe = true;
                flg_response = true; //This flag is temporary bypassed.
                if ((flg_spe && flg_response) == true)
                {
                    button1.Enabled = true;
                }
            }
            else
            {
                flg_spe = false;
            }

        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Open Response matrix
            OpenFileDialog ofd = new OpenFileDialog();


            ofd.FileName = "rmf.dat";
            ofd.Filter = "dat file (*.dat)|*.dat|all file (*.*)|*.*";
            ofd.Title = "Open response matrix file (Mitsubishi format)";
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                rm_filepath = ofd.FileName;
                string tmp = "Respons matrix file name is " + Path.GetFileName(rm_filepath) + Environment.NewLine + Environment.NewLine;

                string[] all_data = File.ReadAllLines(rm_filepath);
                textBox1.AppendText(tmp);

                //textBox1.AppendText(response_str[0]);

                flg_response = true;

                if ((flg_spe && flg_response) == true)
                {
                    button1.Enabled = true;
                }
            }
            else
            {
                flg_response = false;
            }
        }

        private void saveAsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            // Save as
        }

        private void button1_Click(object sender, EventArgs e)
        {
            

            Matrix<double> res_mat = ResponsMatrixParser();

            double[] bin_ene;
            bin_ene=GenerateStandardEnergyBin(300, 10.0);

            FileStream fs = new FileStream(spe_filepath, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs);
            string tmp = sr.ReadLine(); //skip header

            int ite_num = int.Parse(textBox3.Text);
            double updating_limit = double.Parse(textBox4.Text);
            bool flg_smooth = checkBox1.Checked;
            int NLeft, NRight, degree;



            NLeft = int.Parse(textBox2.Text);
            NRight = int.Parse(textBox5.Text);
            degree = int.Parse(textBox6.Text);

           

            for (int spe_now = 0; spe_now < num_spe; spe_now++)
            {

                double meas_time = 0;
                double actv_Cs134 = 0;
                double actv_Cs137 = 0;
                double actv_K40 = 0;

                Vector<double> original_measured_spe = Vector<double>.Build.DenseOfArray(new double[ene_num]);    //M(0)
                //Vector<double> preprocessed_spe = Vector<double>.Build.DenseOfArray(new double[ene_num]);

                original_measured_spe = SingleLineSpeParser(sr.ReadLine(), ref meas_time, ref actv_Cs134, ref actv_Cs137, ref actv_K40);

                textBox1.AppendText("measurment_time, and activitiy of Cs134, Cs137 and K40 of the current spectrum are " + meas_time.ToString() +", "
                   + actv_Cs134.ToString()+", "+ actv_Cs137.ToString()+",and" +actv_K40.ToString());

                if(flg_smooth)
                {
                    double[] single_spe_array = new SavitzkyGolayFilter(degree, NLeft).Process(original_measured_spe.ToArray());
                    original_measured_spe = Vector<double>.Build.DenseOfArray(single_spe_array);

                }
                else
                {
                     //(No smoothing)
                }

                //Start Unfolding Process
                Vector<double> current_emission_spe = original_measured_spe;    //S(i)
                Vector<double> folding_measured_spe = Vector<double>.Build.DenseOfArray(new double[ene_num]);   //M(i)
                Vector<double> previous_measured_spe =Vector<double>.Build.DenseOfArray(new double[ene_num]);   //M(i-1)
                Vector<double> next_emission_spe = Vector<double>.Build.DenseOfArray(new double[ene_num]);      //S(i+1)
                double[] original_measured_spe_array = original_measured_spe.ToArray();
                double[] unfolding_result = new double[ene_num];
                double lower_val_limit = 1E-37;
                //double upper_val_limit = 1E+37;
                int conv_flag = 0;
                double[] next_emission_spe_array = new double[ene_num];
                double[] folding_measured_spe_array = folding_measured_spe.ToArray();
                double[] current_emission_spe_array = current_emission_spe.ToArray();


                double[] update_val_chart = new double[ite_num];
                for(int i=0;i<ite_num;i++)
                {
                    update_val_chart[i] = 0;
                }


                chart1.Series["diff_s2s1"].Points.Clear();

                for (int ite_now = 0; ite_now < ite_num; ite_now++)
                {
                    chart2.["original"]

                    folding_measured_spe = res_mat * current_emission_spe;

                    //next_emission_spe = Vector<double>.op_DotDivide(origina_measured_spe, folding_measured_spe);




                    for (int ix = 0; ix < ene_num; ix++)
                    {
                        if ((original_measured_spe_array[ix] > lower_val_limit) && (folding_measured_spe_array[ix] > lower_val_limit))
                        {
                            next_emission_spe_array[ix] = original_measured_spe_array[ix] / folding_measured_spe_array[ix] * current_emission_spe_array[ix];
                         }
                        else
                        {
                            next_emission_spe_array[ix] = 0.0;
                        }
                    }
                    next_emission_spe = Vector<double>.Build.DenseOfArray(next_emission_spe_array);
                    //next_emission_spe = Vector<double>.op_DotMultiply(next_emission_spe, current_emission_spe);

                    //Checking convergence condition
                    double updating_average = 0;
                    for(int ix=0;ix<ene_num;ix++)
                    {
                        updating_average += Math.Abs(next_emission_spe_array[ix] - current_emission_spe_array[ix]);
                    }
                   
                    updating_average = updating_average / ene_num;
                    current_emission_spe = next_emission_spe;
                    current_emission_spe_array = current_emission_spe.ToArray();
                    
                    if (ite_now % 10 == 0)
                    {
                        chart1.Series["diff_s2s1"].Points.AddXY(ite_now, updating_average);
                        chart1.Update();
                    }

                    //update_val_chart[ite_now] = updating_average;

                    chart2.Series["unfoldingResult"].Points.Clear();
                    chart2.Series["foldingResult"].Points.Clear();
                    if (ite_now%10==0)
                    {
                        for (int i = 0; i < ene_num; i++)
                        {
                            chart2.Series["unfoldingResult"].Points.AddXY(bin_ene[i], current_emission_spe_array[i]);
                            chart2.Series["foldingResult"].Points.AddXY(bin_ene[i],folding_measured_spe_array[i]);
                        }
                        chart2.Update();
                    }
              

                    if (updating_average <updating_limit)
                    {
                        unfolding_result = current_emission_spe.ToArray(); ;
                        conv_flag = 2;
                        
                        break;
                    }
                    
                    if(ite_now==(ite_num-1))
                    {
                        unfolding_result = current_emission_spe.ToArray();
                        conv_flag = 1;
                    }
                }

                textBox1.AppendText((spe_now + 1).ToString() + " /(" + num_spe.ToString() + ") finished." + Environment.NewLine + Environment.NewLine);
                //textBox1.AppendText(current_emission_spe.ToString() + " " + conv_flag.ToString() + " <- conv. flag \n");
                bool overwrite_or_append = false;
                if(spe_now==0)
                {
                    overwrite_or_append = false;
                        }
                else
                {
                    overwrite_or_append = true;
                }

                StreamWriter sw = new StreamWriter(textBox7.Text,overwrite_or_append, System.Text.Encoding.Default);
                string single_output = "";
                // chart2.Series["unfoldingResult"].Points.Clear();
                double[] unfolding_result_array = unfolding_result.ToArray();

                for (int i =0; i<ene_num; i++)
                {
                    single_output += unfolding_result_array[i] +", "+Environment.NewLine;
                    //chart2.Series["unfoldingResult"].Points.AddXY(bin_ene[i], emission_spe_array[i]);
                }
                //chart2.Update();
                sw.WriteLine("Cs-134: "+ actv_Cs134.ToString()+" Bq / Cs-137: " +actv_Cs137.ToString() + ", K40: " + actv_K40.ToString() +
                    Environment.NewLine +  single_output +Environment.NewLine+ " conv. criterion ID: "+ Environment.NewLine +conv_flag.ToString() + Environment.NewLine + Environment.NewLine);
                sw.Close();

                

            }

        }



        private double[] GenerateStandardEnergyBin(int maxch, double bin_width)
        {
            double[] lower_bin_ene = new double[maxch];
            for(int i=0; i<maxch;i++)
            {
                lower_bin_ene[i] = bin_width * i;
            }
            return lower_bin_ene;
        }

        private Vector<double> SingleLineSpeParser(string str, ref double meas_time, ref double actv_Cs134, ref double actv_Cs137, ref double actv_K40)
        {

            string[] separatedStr = str.Split(',');
            meas_time = double.Parse(separatedStr[0]);
            actv_Cs134 = double.Parse(separatedStr[1]);
            actv_Cs137 = double.Parse(separatedStr[2]);
            actv_K40 = double.Parse(separatedStr[3]);

            double[] tmp = new double[ene_num];
            for (int i = 4; i < separatedStr.Length - 4; i++)
            {
                tmp[i - 4] = double.Parse(separatedStr[i]);
            }
            return Vector<double>.Build.DenseOfArray(tmp);
        }

        private Matrix<double> ResponsMatrixParser()
        {


            string[] all_data = File.ReadAllLines(rm_filepath);
            res_num = all_data.Length;

            string tmp = Regex.Replace(all_data[0], @"\s+", " ");
            tmp = Regex.Replace(tmp, @"^ +", "");
            string[] tmp2 = tmp.Split(' ');
            ene_num = tmp2.Length;

            double[,] responseMatrix = new double[res_num, ene_num];

            for (int i = 0; i < res_num; i++)
            {
                string duplicate_space_removed_str = Regex.Replace(all_data[i], @"\s+", " ");
                duplicate_space_removed_str = Regex.Replace(duplicate_space_removed_str, @"^ +", "");
                string[] separatedStr = duplicate_space_removed_str.Split(' ');
                for (int j = 0; j < ene_num; j++)
                {
                    responseMatrix[i, j] = double.Parse(separatedStr[j]);
                }
            }

            return Matrix<double>.Build.DenseOfArray(responseMatrix);

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            textBox5.Text = textBox2.Text;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

    }


}



namespace Filtering
{
    /// <summary>
    /// <para>Implements a Savitzky-Golay smoothing filter, as found in [1].</para>
    /// <para>[1] Sophocles J.Orfanidis. 1995. Introduction to Signal Processing. Prentice-Hall, Inc., Upper Saddle River, NJ, USA.</para>
    /// </summary>
    public sealed class SavitzkyGolayFilter
    {
        private readonly int sidePoints;

        private Matrix<double> coefficients;

        public SavitzkyGolayFilter(int sidePoints, int polynomialOrder)
        {
            this.sidePoints = sidePoints;
            Design(polynomialOrder);
        }

        /// <summary>
        /// Smoothes the input samples.
        /// </summary>
        /// <param name="samples"></param>
        /// <returns></returns>
        public double[] Process(double[] samples)
        {
            int length = samples.Length;
            double[] output = new double[length];
            int frameSize = (sidePoints << 1) + 1;
            double[] frame = new double[frameSize];

            Array.Copy(samples, frame, frameSize);

            for (int i = 0; i < sidePoints; ++i)
            {
                output[i] = coefficients.Column(i).DotProduct(Vector<double>.Build.DenseOfArray(frame));
            }

            for (int n = sidePoints; n < length - sidePoints; ++n)
            {
                Array.ConstrainedCopy(samples, n - sidePoints, frame, 0, frameSize);
                output[n] = coefficients.Column(sidePoints).DotProduct(Vector<double>.Build.DenseOfArray(frame));
            }

            Array.ConstrainedCopy(samples, length - frameSize, frame, 0, frameSize);

            for (int i = 0; i < sidePoints; ++i)
            {
                output[length - sidePoints + i] = coefficients.Column(sidePoints + 1 + i).DotProduct(Vector<double>.Build.Dense(frame));
            }

            return output;
        }

        private void Design(int polynomialOrder)
        {
            double[,] a = new double[(sidePoints << 1) + 1, polynomialOrder + 1];

            for (int m = -sidePoints; m <= sidePoints; ++m)
            {
                for (int i = 0; i <= polynomialOrder; ++i)
                {
                    a[m + sidePoints, i] = Math.Pow(m, i);
                }
            }

            Matrix<double> s = Matrix<double>.Build.DenseOfArray(a);
            coefficients = s.Multiply(s.TransposeThisAndMultiply(s).Inverse()).Multiply(s.Transpose());
        }
    }
}