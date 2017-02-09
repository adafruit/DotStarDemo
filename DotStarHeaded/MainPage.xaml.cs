/*------------------------------------------------------------------------
  Windows IoT Core demonstration app for Adafruit DotStar Strips

  Requires the Adafruit Class Library for Windows IoT Core

  Written by Rick Lesniak for Adafruit Industries.

  Adafruit invests time and resources providing this open source code,
  please support Adafruit and open-source hardware by purchasing products
  from Adafruit!

  ------------------------------------------------------------------------
  Adafruit DotStarHeaded is free software: you can redistribute it and/or
  modify it under the terms of the GNU Lesser General Public License
  as published by the Free Software Foundation, either version 3 of
  the License, or (at your option) any later version.

  Adafruit DotStarHeaded is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU Lesser General Public License for more details.

  You should have received a copy of the GNU Lesser General Public
  License along with DotStar.  If not, see <http://www.gnu.org/licenses/>.
  ------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using AdafruitClassLibrary;
using System.Runtime;
using System.Threading;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DotStarApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //DotStar strip = new DotStar(60, 12, 13, DotStar.DOTSTAR_BGR);    //software SPI.  Data on GPIO 12, Clock on GPIO 13
        DotStar strip = new DotStar(60, DotStar.DOTSTAR_BGR);    //hardware SPI.  Data on GPIO 10, Clock on GPIO 11
        
        // Declare a System.Threading.CancellationTokenSource.
        CancellationTokenSource cts;// = new CancellationTokenSource();

        bool notRunning = true;

        public MainPage()
        {
            this.InitializeComponent();
            //StrandTest();
            //Task.Run(() => StrandTest()).Wait(cts.Token);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            notRunning = true;
            StartButton_Click(sender, e);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (notRunning)
            {
                cts = new CancellationTokenSource();
                Task.Run(() => StrandTest(cts.Token), cts.Token);
                notRunning = false;
                StartButton.IsEnabled = false;
                StopButton.IsEnabled = true;
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
            StopButton.IsEnabled = false;
            StartButton.IsEnabled = true;
        }

        private void Delay(int time, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();      //throws an exception if the task has been canceled
            Task.Delay(time).Wait();
        }

        public async Task  StrandTest(CancellationToken ct)
        {
            await strip.BeginAsync().ConfigureAwait(false);
            try
            {
                while (!ct.IsCancellationRequested)
                //while (true)
                {
                    RGBRun(3, 20, ct);  //Color wipes

                    TheaterChase(strip.Color(127, 127, 127), 50, 10, ct); // White
                    TheaterChase(strip.Color(127, 0, 0), 50, 10, ct); // Red
                    TheaterChase(strip.Color(0, 0, 127), 50, 10, ct); // Blue

                    Rainbow(20, ct);
                    RainbowCycle(20, ct);
                    TheaterChaseRainbow(50, ct);
                }
            }
            catch (Exception ex)
            {
                //we'll eventually get here on a task cancellation.  See Delay function above
            }

            strip.Clear();
            strip.Show();
            strip.End();
            notRunning = true;
            StartButton.IsEnabled = true;
        }

        public void RGBRun(int iterations, int wait, CancellationToken ct)
        {
            int Head = 0;
            int Tail = -10;
            UInt32 Color = strip.Color(0, 0, 255);  //start with blue

            strip.Brightness = 255;  //start at full brightness

            //turn all pixels off
            strip.Clear();
            strip.Show();

            //run the color wipes
            while (iterations > 0)
            {
                strip.SetPixelColor(Head, Color); // 'On' pixel at head
                strip.SetPixelColor(Tail, 0);     // 'Off' pixel at tail
                strip.Show();                     // Refresh strip

                //Task.Delay(wait).Wait();
                Delay(wait, ct);

                if (++Head >= strip.NumPixels)
                {         // Increment head index.  Off end of strip?
                    Head = 0;                       //  Yes, reset head index to start
                    strip.Brightness = (byte)(strip.Brightness / 2);
                    if ((Color >>= 8) == 0)          //  Next color (R->G->B) ... past blue now?
                    {
                        Color = strip.Color(255, 0, 0);  //   Yes, reset to red
                        iterations--;
                    }
                }

                if (++Tail >= strip.NumPixels)
                {
                    Tail = 0; // Increment, reset tail index
                }
            }

            strip.Brightness = 255;  //Reset brightness

        }

        public void Rainbow(int wait, CancellationToken ct)
        {
            for (int cycle = 0; cycle < 256; cycle++)
            {
                for (int pixel = 0; pixel < strip.NumPixels; pixel++)
                {
                    strip.SetPixelColor(pixel, Wheel((uint)(pixel + cycle) & 255));
                }
                strip.Show();
                //Task.Delay(wait).Wait();
                Delay(wait, ct);
            }
        }

        // Slightly different, this makes the rainbow equally distributed throughout
        public void RainbowCycle(int wait, CancellationToken ct)
        {
            for (int cycle = 0; cycle < 256 * 5; cycle++)
            {
                for (int pixel = 0; pixel < strip.NumPixels; pixel++)
                {
                    strip.SetPixelColor(pixel, Wheel((uint)((pixel * 256 / strip.NumPixels) + cycle) & 255));
                }
                strip.Show();
                //Task.Delay(wait).Wait();
                Delay(wait, ct);
            }
        }

        //Theatre-style crawling lights 
        public void TheaterChase(uint color, int wait, int cycleCount, CancellationToken ct)
        {
            for (int cycle = 0; cycle < cycleCount; cycle++)
            {
                for (int offset = 0; offset < 3; offset++)
                {
                    for (int pixel = 0; pixel < strip.NumPixels; pixel = pixel + 3)
                    {
                        strip.SetPixelColor(pixel + offset, color);    //turn every third pixel on
                    }
                    strip.Show();

                    //Task.Delay(wait).Wait();
                    Delay(wait, ct);

                    for (int pixel = 0; pixel < strip.NumPixels; pixel = pixel + 3)
                    {
                        strip.SetPixelColor(pixel + offset, 0);        //turn every third pixel off
                    }
                }
            }
        }

        //Theatre-style crawling lights with rainbow effect
        public void TheaterChaseRainbow(int wait, CancellationToken ct)
        {
            for (int cycle = 0; cycle < 256; cycle++)  // cycle all 256 colors in the wheel
            {
                for (int offset = 0; offset < 3; offset++)
                {
                    for (int i = 0; i < strip.NumPixels; i = i + 3)
                    {
                        strip.SetPixelColor(i + offset, Wheel((uint)(i + cycle) % 255));    //turn every third pixel on
                    }
                    strip.Show();

                    //Task.Delay(wait).Wait();
                    Delay(wait, ct);

                    for (int i = 0; i < strip.NumPixels; i = i + 3)
                    {
                        strip.SetPixelColor(i + offset, 0);        //turn every third pixel off
                    }
                }
            }
        }

        // Input a value 0 to 255 to get a color value.
        // The colors are a transition r - g - b - back to r.
        public uint Wheel(uint WheelPos)
        {
            if (WheelPos < 85)
            {
                return strip.Color((WheelPos * 3), (255 - WheelPos * 3), 0);
            }
            else if (WheelPos < 170)
            {
                WheelPos -= 85;
                return strip.Color((255 - WheelPos * 3), 0, (WheelPos * 3));
            }
            else
            {
                WheelPos -= 170;
                return strip.Color(0, (WheelPos * 3), (255 - WheelPos * 3));
            }
        }
    }
}
