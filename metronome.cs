using System;
using System.Collections.Generic;
using Microsoft.Kinect;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Media;
using System.Diagnostics;

namespace Moto
{
    public partial class metronome
    {
        static Stopwatch beatTimer;
        public static DispatcherTimer theMetronome;
        static long beatCount = 0;
        static long beatTime = 0;

        static long timePile = 0;

        static SoundPlayer metronomeTick = new SoundPlayer("metronome-tick.wav");

        public static void setupMetronome() {
            beatTimer = new Stopwatch();
            theMetronome = new DispatcherTimer();
            theMetronome.Interval = TimeSpan.FromMilliseconds(1000);
            theMetronome.Tick += new EventHandler(metronome_Tick);
        }

        static void metronome_Tick(object sender, EventArgs e)
        {
            metronomeTick.Play();
        }

        public static void metronomeBeat()
        {
            if (beatTimer.IsRunning)
            {
                beatTimer.Stop();


                //We're listening for beats, do the number crunching
                beatCount++;

                timePile += beatTimer.ElapsedMilliseconds;

                beatTime = timePile / beatCount;

                if (beatCount > 1)
                {
                    setMetronome(beatTime);
                }
            }


            beatTimer.Restart();
        }

        private static void setMetronome(double milSec)
        {
            theMetronome.Stop();
            theMetronome.Interval = TimeSpan.FromMilliseconds(milSec);
            theMetronome.Start();

            metronomeTick.Play();
        }

        public static void destroyMetronome()
        {
            if (theMetronome != null)
            {
                theMetronome.Stop();
                theMetronome.Tick -= metronome_Tick;
                theMetronome = null;
            }
        }
    }
}