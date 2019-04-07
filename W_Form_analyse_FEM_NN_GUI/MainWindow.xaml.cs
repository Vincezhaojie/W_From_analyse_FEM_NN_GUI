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

using SolidWorks.Interop.cosworks;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System.Runtime.InteropServices;
using System.Collections;
using System.Threading;
using Excel = Microsoft.Office.Interop.Excel;
using System.Timers;


namespace W_Form_analyse_FEM_NN_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        static string dir, modelFile;
        float[,] testArray = new float[1, 9] {{1f, 0.5f,0.625f,0.2857f,1f,0.857f,0.857f,0.347f,0.837f}};
        SldWorks swApp;

        public MainWindow()
        {
            InitializeComponent();
            dir = "D:\\TUD\\7.Semeter\\SA\\SA_code\\c#\\W_Form_analyse_FEM_NN_GUI\\W_Form_analyse_FEM_NN_GUI";
            #region start SW
            
            Console.WriteLine("starting SolidWorks");
            try
            {
                swApp = (SldWorks)Marshal.GetActiveObject("SldWorks.Application");
            }
            catch (Exception)
            {
                swApp = new SldWorks();
                swApp.Visible = false;
            }
            Console.WriteLine("SolidWorks successfully started");
            
            #endregion
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
            testArray[0, 0] = (testArray[0, 0] - 2) / (4 - 2);
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
                runner.AddInput(graph["dense_input"][0], tensor).Fetch(graph["dense_5/BiasAdd"][0]);
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
            #region Variable definition
            
            int m;//how many cells
            int Count; // how many data
            double width, thickness, l1, l2, l3, F1, F2, F3;
            double l4, l1_, l2_, l3_, l4_, cell_length, cell_height;
            double x_o = 0.0;
            double y_o = 0.0;
            double x_o_ = 0.0;
            double y_o_ = 0.0;
            const double MeshEleSize = 2.0;
            const double MeshTol = 0.1;
            string strMaterialLib = null;
            object[] Disp = null;
            object[] Stress = null;
            ModelDoc2 swModel = null;
            SelectionMgr selectionMgr = null;
            CosmosWorks COSMOSWORKS = null;
            CwAddincallback COSMOSObject = default(CwAddincallback);
            CWModelDoc ActDoc = default(CWModelDoc);
            CWStudyManager StudyMngr = default(CWStudyManager);
            CWStudy Study = default(CWStudy);
            CWSolidManager SolidMgr = default(CWSolidManager);
            CWSolidBody SolidBody = default(CWSolidBody);
            CWSolidComponent SolidComp = default(CWSolidComponent);


            CWForce cwForce = default(CWForce);
            CWLoadsAndRestraintsManager LBCMgr = default(CWLoadsAndRestraintsManager);
            CWResults CWFeatobj = default(CWResults);
            bool isSelected;
            float maxDisp = 0.0f;
            float maxStress = 0.0f;
            int intStatus = 0;
            int errors = 0;
            int errCode = 0;
            int warnings = 0;



            int ran_m, ran_width, ran_thickness, ran_l1, ran_l2, ran_l3, ran_F1, ran_F2, ran_F3;
            Excel.Application exlApp;
            Excel.Workbook exlBook;
            Excel.Worksheet exlSheet;



            #endregion

            #region read data from UI
            try
            {
                m = int.Parse(textBox_hm_cells.Text);
                width = double.Parse(textBox_width.Text) / 1000.0;
                thickness = double.Parse(textBox_thickness.Text) / 1000.0;
                l1 = double.Parse(textBox_l1.Text) / 1000.0;
                l2 = double.Parse(textBox_l2.Text) / 1000.0;
                l3 = double.Parse(textBox_l3.Text) / 1000.0;
                F1 = double.Parse(textBox_F1.Text);
                F2 = double.Parse(textBox_F2.Text);
                F3 = double.Parse(textBox_F3.Text);
            }
            catch (Exception)
            {
                MessageBox.Show("input format error, check input data");
                return;
            }

            l4 = l2;
            l1_ = l1 - 2 * width;
            l2_ = l2;
            l3_ = l3 + 2 * width;
            l4_ = l4;
            cell_length = l1 + l3;
            cell_height = l2 + width;
            #endregion

            #region geometrie
            Console.WriteLine("Start creating new Germetrie");
            swModel = swApp.NewPart();
            swModel.Extension.SelectByID("前视基准面", "PLANE", 0, 0, 0, false, 1, null);
            swModel.InsertSketch2(true);
            #region sketch
            for (int i = 0; i < m; i++)
            {
                if (i == 0)
                {
                    swModel.SketchManager.CreateLine(0, 0, 0, l1 - width, 0, 0);
                    swModel.SketchManager.CreateLine(l1 - width, 0, 0, l1 - width, -l2, 0);
                    swModel.SketchManager.CreateLine(l1 - width, -l2, 0, l1 + l3 - width, -l2, 0);
                    swModel.SketchManager.CreateLine(l1 + l3 - width, -l2, 0, l1 + l3 - width, 0, 0);

                    swModel.SketchManager.CreateLine(0, 0, 0, 0, -width, 0);
                    swModel.SketchManager.CreateLine(0, -width, 0, l1_, -width, 0);
                    swModel.SketchManager.CreateLine(l1_, -width, 0, l1_, -(width + l2_), 0);
                    swModel.SketchManager.CreateLine(l1_, -(width + l2_), 0, l1_ + l3_, -(width + l2_), 0);
                    swModel.SketchManager.CreateLine(l1_ + l3_, -(width + l2_), 0, l1_ + l3_, -width, 0);
                }
                else
                {
                    x_o = i * cell_length - width;
                    y_o = 0;
                    x_o_ = x_o + width;
                    y_o_ = y_o - width;

                    swModel.SketchManager.CreateLine(x_o, y_o, 0, x_o + l1, 0, 0);
                    swModel.SketchManager.CreateLine(x_o + l1, 0, 0, x_o + l1, -l2, 0);
                    swModel.SketchManager.CreateLine(x_o + l1, -l2, 0, x_o + l1 + l3, -l2, 0);
                    swModel.SketchManager.CreateLine(x_o + l1 + l3, -l2, 0, x_o + l1 + l3, 0, 0);

                    swModel.SketchManager.CreateLine(x_o_, y_o_, 0, x_o_ + l1_, y_o_, 0);
                    swModel.SketchManager.CreateLine(x_o_ + l1_, y_o_, 0, x_o_ + l1_, y_o_ - l2_, 0);
                    swModel.SketchManager.CreateLine(x_o_ + l1_, y_o_ - l2_, 0, x_o_ + l1_ + l3_, y_o_ - l2_, 0);
                    swModel.SketchManager.CreateLine(x_o_ + l1_ + l3_, y_o_ - l2_, 0, x_o_ + l1_ + l3_, y_o_, 0);
                }
            }
            swModel.SketchManager.CreateLine(x_o_ + l1_ + l3_, y_o_, 0, x_o_ + l1_ + l3_, 0, 0);
            swModel.SketchManager.CreateLine(x_o_ + l1_ + l3_, 0, 0, x_o + l1 + l3, 0, 0);

            #endregion
            swModel.FeatureManager.FeatureExtrusion2(
                    true, false, false, 0, 0, thickness, 0, false, false, false, false, 0, 0, false, false, false, false, true, true, true, 0, 0, true
                    );

            swModel.SaveAsSilent("D:\\TUD\\7.Semeter\\SA\\SA_code\\c#\\W_Form_analyse_FEM_NN_GUI\\Geometrie.sldprt", true);
            swApp.CloseAllDocuments(true);
            swApp.OpenDoc("D:\\TUD\\7.Semeter\\SA\\SA_code\\c#\\W_Form_analyse_FEM_NN_GUI\\Geometrie.sldprt", (int)swOpenDocOptions_e.swOpenDocOptions_Silent);
            Console.WriteLine("Geometrie success");


            #endregion

            #region simulaiton
            string path_to_cosworks_dll = @"C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\Simulation\cosworks.dll";
            errors = swApp.LoadAddIn(path_to_cosworks_dll);
            COSMOSObject = (CwAddincallback)swApp.GetAddInObject("SldWorks.Simulation");
            try
            {
                COSMOSWORKS = (CosmosWorks)COSMOSObject.CosmosWorks;
            }
            catch (Exception)
            {
                Console.WriteLine("something wrong in Simulaiton Add In, start a new one");
                swApp.CloseAllDocuments(true);
                
                return;
            }


            COSMOSWORKS = COSMOSObject.CosmosWorks;
            //Get active document
            ActDoc = (CWModelDoc)COSMOSWORKS.ActiveDoc;

            //Create new static study
            StudyMngr = (CWStudyManager)ActDoc.StudyManager;
            Study = (CWStudy)StudyMngr.CreateNewStudy("static study", (int)swsAnalysisStudyType_e.swsAnalysisStudyTypeStatic, 0, out errCode);

            //Add materials
            //get MaterialLib
            strMaterialLib = swApp.GetExecutablePath() + "\\lang\\english\\sldmaterials\\solidworks materials.sldmat";
            SolidMgr = Study.SolidManager;
            SolidComp = SolidMgr.GetComponentAt(0, out errCode);
            SolidBody = SolidComp.GetSolidBodyAt(0, out errCode);
            intStatus = SolidBody.SetLibraryMaterial(strMaterialLib, "AISI 1020");

            //fixed restraints
            LBCMgr = Study.LoadsAndRestraintsManager;

            swModel = (ModelDoc2)swApp.ActiveDoc;
            swModel.ShowNamedView2("", (int)swStandardViews_e.swIsometricView);

            selectionMgr = (SelectionMgr)swModel.SelectionManager;
            isSelected = swModel.Extension.SelectByID2("", "FACE", 0, -width / 2.0, thickness / 2.0, false, 0, null, 0);
            if (isSelected)
            {
                object selectedFace = (object)selectionMgr.GetSelectedObject6(1, -1);
                object[] fixedFaces = { selectedFace };
                CWRestraint restraint = (CWRestraint)LBCMgr.AddRestraint((int)swsRestraintType_e.swsRestraintTypeFixed, fixedFaces, null, out errCode);
            }
            swModel.ClearSelection2(true);

            //add force
            selectionMgr = (SelectionMgr)swModel.SelectionManager;
            isSelected = swModel.Extension.SelectByID2("", "FACE", x_o + l1 + l3 + (width / 2.0), 0, thickness / 2.0, false, 0, null, 0);
            if (isSelected)
            {
                object selectedFace = (object)selectionMgr.GetSelectedObject6(1, -1);
                object[] forceAdd = { selectedFace };
                selectionMgr = (SelectionMgr)swModel.SelectionManager;
                swModel.Extension.SelectByID2("", "FACE", x_o + l1 + l3 + width, -(l4 + width) / 2.0, thickness / 2.0, false, 0, null, 0);
                object selectedFaceToForceDir = (object)selectionMgr.GetSelectedObject6(1, -1);
                double[] distValue = null;
                double[] forceValue = null;
                double[] Force = { F2, F3, F1 };
                cwForce = (CWForce)LBCMgr.AddForce3((int)swsForceType_e.swsForceTypeForceOrMoment, (int)swsSelectionType_e.swsSelectionFaceEdgeVertexPoint,
                                                    2, 0, 0, 0,
                                                    (distValue),
                                                    (forceValue),
                                                    false, true,
                                                    (int)swsBeamNonUniformLoadDef_e.swsTotalLoad,
                                                    0, 7, 0.0,
                                                    Force,
                                                    false, false,
                                                    (forceAdd),
                                                    (selectedFaceToForceDir),
                                                    false, out errCode);//i have tried to figure out these arguments for one day, keep them and dont't change them.
                                                                        //the way to check cwForce : cwForce.GetForceComponentValues()
                                                                        //ForceComponet: [int b1, // 1 if x-direction hat Force-komponent, else 0
                                                                        //                int b2, // 1 if y-direction hat Force-komponent, else 0
                                                                        //                int b3, // 1 if z-direction hat Force-komponent, else 0
                                                                        //                double d1, // Force-komponent in x
                                                                        //                double d2, // Force-komponent in y
                                                                        //                double d3 // Force-komponent in z
                                                                        //                          ]
                                                                        //PS: the definition of xyz seems like not the same as the global XYZ system in SW.
                swModel.ClearSelection2(true);

                //meshing
                CWMesh CWMeshObj = default(CWMesh);
                CWMeshObj = Study.Mesh;
                CWMeshObj.MesherType = (int)swsMesherType_e.swsMesherTypeStandard;
                CWMeshObj.Quality = (int)swsMeshQuality_e.swsMeshQualityDraft;
                errCode = Study.CreateMesh(0, MeshEleSize, MeshTol);
                CWMeshObj = null;

                //run analysis
                errCode = Study.RunAnalysis();
                if (errCode != 0)
                {
                    Console.WriteLine(string.Format("RunAnalysis errCode = {0}", errCode));
                    Console.WriteLine(string.Format("RunAnalysis failed"));
                    
                    errors = swApp.UnloadAddIn(path_to_cosworks_dll);
                    swApp.CloseAllDocuments(true);
                    Console.WriteLine(string.Format("please start a new one"));
                    return;
                }
                Console.WriteLine("RunAnalysis successed, ready to get results");

                //get results
                CWFeatobj = Study.Results;
                //get max von Mieses stress
                Stress = (object[])CWFeatobj.GetMinMaxStress((int)swsStressComponent_e.swsStressComponentVON,
                                                             0, 0, null,
                                                             (int)swsStrengthUnit_e.swsStrengthUnitNewtonPerSquareMillimeter,
                                                             out errCode);
                maxStress = (float)Stress[3]; //Stress: {node_with_minimum_stress, minimum_stress, node_with_maximum_stress, maximum_stress}
                Console.WriteLine(maxStress);
                if (maxStress >= 351.6)
                {
                    Console.WriteLine("out of yield stress, start a new example");
                    errors = swApp.UnloadAddIn(path_to_cosworks_dll);
                    swApp.CloseAllDocuments(true);
                    textBox_FEM_result.Text = "out of yield stress";
                    return;
                }
                //get max URES displacement
                Disp = (object[])CWFeatobj.GetMinMaxDisplacement((int)swsDisplacementComponent_e.swsDisplacementComponentURES,
                                                                 0, null,
                                                                 (int)swsLinearUnit_e.swsLinearUnitMillimeters,
                                                                 out errCode);
                maxDisp = (float)Disp[3]; //Disp: {node_with_minimum_displacement, minimum_displacement, node_with_maximum_displacement, maximum_displacement}
                CWFeatobj = null;
                Console.WriteLine(string.Format("max Displacement: {0:f4} mm", maxDisp));
                textBox_FEM_result.Text = maxDisp.ToString();
                
            }
            else
            {
                Console.WriteLine("not selected");
            }
            #endregion
            errors = swApp.UnloadAddIn(path_to_cosworks_dll);
            swApp.CloseAllDocuments(true);
          
        }

        private void Button_get_error_Click(object sender, RoutedEventArgs e)
        {
            double FEM, NN, error;
            try
            {
                FEM = double.Parse(textBox_FEM_result.Text);
                NN = double.Parse(textBox_NN_result.Text);
            }
            catch (Exception)
            {
                Console.WriteLine("FEM or NN not calculated");
                return;
            }

            error = Math.Abs(FEM - NN);
            textBox_Error.Text = error.ToString();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            swApp.ExitApp();
        }

        static void ModelFiles(string dir)
        {
            modelFile = System.IO.Path.Combine(dir, "W_Form_NN_model.pb");
        }
    }
}
