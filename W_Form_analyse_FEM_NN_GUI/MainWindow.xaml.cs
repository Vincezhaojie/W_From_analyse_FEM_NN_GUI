using System;
using System.Collections.Generic;
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

using TensorFlow;
using System.IO;


namespace W_Form_analyse_FEM_NN_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        static string dir, modelFile;
        float[,] testArray = new float[1, 9] {{1f, 0.5f,0.625f,0.2857f,1f,0.857f,0.857f,0.347f,0.837f}};

        public MainWindow()
        {
            InitializeComponent();
            dir = "D:\\TUD\\7.Semeter\\SA\\SA_code\\c#\\W_Form_analyse_FEM_NN_GUI\\W_Form_analyse_FEM_NN_GUI";
        }

        private void button_generateNN_Click(object sender, RoutedEventArgs e)
        {
            #region data pre-processing
            try
            {
                testArray[0, 0] = float.Parse(textBox_hm_cells.Text);
                testArray[0, 1] = float.Parse(textBox_width.Text);
                testArray[0, 2] = float.Parse(textBox_thickness.Text);
                testArray[0, 3] = float.Parse(textBox_l1.Text);
                testArray[0, 4] = float.Parse(textBox_l2.Text);
                testArray[0, 5] = float.Parse(textBox_l3.Text);
                testArray[0, 6] = float.Parse(textBox_F1.Text);
                testArray[0, 7] = float.Parse(textBox_F2.Text);
                testArray[0, 8] = float.Parse(textBox_F3.Text);
            }
            catch (Exception)
            {
                MessageBox.Show("input format error, check input data");
                return;
            }

            //MinMaxScaler
            testArray[0, 0] = (testArray[0, 0] - 2) / (14 - 2);
            testArray[0, 1] = (testArray[0, 1] - 3) / (9 - 3);
            testArray[0, 2] = (testArray[0, 2] - 1) / (9 - 1);
            testArray[0, 3] = (testArray[0, 3] - 24) / (31 - 24);
            testArray[0, 4] = (testArray[0, 4] - 15) / (20 - 15);
            testArray[0, 5] = (testArray[0, 5] - 14) / (21 - 14);
            testArray[0, 6] = (testArray[0, 6] - 0) / (49 - 0);
            testArray[0, 7] = (testArray[0, 7] - 0) / (49 - 0);
            testArray[0, 8] = (testArray[0, 8] - 0) / (49 - 0);
            #endregion

            ModelFiles(dir);

            var model = File.ReadAllBytes(modelFile);
            TFGraph graph = new TFGraph();

            graph.Import(model, "");


            using (var session = new TFSession(graph))
            {
                var tensor = CreateTensorFromArray(testArray);
                var runner = session.GetRunner();
                runner.AddInput(graph["dense_input"][0], tensor).Fetch(graph["dense_4/BiasAdd"][0]);
                var output = runner.Run();
                var result = output[0];
                var val = (float[,])result.GetValue(jagged: false);

                textBox_NN_result.Text = val[0, 0].ToString();
                Console.WriteLine(val[0, 0]);
            }
        }

        public static TFTensor CreateTensorFromArray(float[,] testArr)
        {
            return (TFTensor)testArr;
        }

        private void Button_generateFEM_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_get_error_Click(object sender, RoutedEventArgs e)
        {

        }

        static void ModelFiles(string dir)
        {
            modelFile = System.IO.Path.Combine(dir, "W_Form_NN_model.pb");
        }
    }
}
