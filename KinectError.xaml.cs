using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace Moto
{
    /// <summary>
    /// Interaction logic for KinectError.xaml
    /// </summary>
    public partial class KinectError : Page
    {
        Image kinectError;
        Image restartMsg;
        Image initialisingSpinner;

        public KinectError(KinectStatus status)
        {
            InitializeComponent();

            KinectSensor.KinectSensors.StatusChanged += new EventHandler<StatusChangedEventArgs>(KinectSensors_StatusChanged);

            MainWindow.animateFade(imgDimmer, 0, 0.5);

            kinectError = MainWindow.generateError(status);
            MainCanvas.Children.Add(kinectError);
            MainWindow.animateSlide(kinectError);
        }

        void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (kinectError != null)
            {
                MainWindow.animateSlide(kinectError, true, true);

                if (initialisingSpinner != null)
                {
                    MainWindow.animateSlide(initialisingSpinner, true, true);
                    initialisingSpinner = null;
                }
            }

            if (e.Status == KinectStatus.Connected)
            {
                MainWindow.restartMoto();
            }
            else
            {                
                MainWindow.animateFade(imgDimmer, 0, 0.5);

                kinectError = MainWindow.generateError(e.Status);
                MainCanvas.Children.Add(kinectError);
                MainWindow.animateSlide(kinectError);

                if (e.Status == KinectStatus.Initializing)
                {
                    //Spinny, turny progress animation
                    initialisingSpinner = new Image();
                    initialisingSpinner.Source = new BitmapImage(new Uri(
        "/Moto;component/images/loading.png", UriKind.Relative));
                    MainCanvas.Children.Add(initialisingSpinner);
                    initialisingSpinner.Width = 150;
                    initialisingSpinner.Height = 150;
                    MainWindow.animateSlide(initialisingSpinner);
                    MainWindow.animateSpin(initialisingSpinner);
                    Canvas.SetTop(initialisingSpinner, 230);

                    Canvas.SetLeft(initialisingSpinner, (MainCanvas.ActualWidth / 2) - (initialisingSpinner.Width / 2));
                    //Add a "Moto will restart soon" message
                    restartMsg = new Image();
                    restartMsg.Source = new BitmapImage(new Uri(
        "/Moto;component/images/kinect-fault/restart.png", UriKind.Relative));
                    MainCanvas.Children.Add(restartMsg);
                    restartMsg.Width = 465;
                    restartMsg.Height = 37;
                    MainWindow.animateSlide(restartMsg);
                    Canvas.SetTop(restartMsg, 200);
                    Canvas.SetLeft(restartMsg, (MainCanvas.ActualWidth / 2) - (restartMsg.Width / 2));
                }
                else if (e.Status == KinectStatus.Disconnected)
                {
                    MainWindow.stopKinect(MainWindow.sensor);
                }
            }
        }
    }
}
