using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FaceDetector
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void backgroundWorker1_DoWork( object sender, DoWorkEventArgs e )
        {
            // Do not access the form's BackgroundWorker reference directly.
            // Instead, use the reference provided by the sender parameter.
            BackgroundWorker bw = sender as BackgroundWorker;

            // Extract the argument.
            int arg = ( int ) e.Argument;

            // Start the time-consuming operation.
            e.Result = TimeConsumingOperation( bw, arg );

            // If the operation was canceled by the user, 
            // set the DoWorkEventArgs.Cancel property to true.
            if ( bw.CancellationPending )
            {
                e.Cancel = true;
            }
        }

        private int TimeConsumingOperation( BackgroundWorker bw, int sleepPeriod )
        {
            int result = 0;

            Random rand = new Random();

            while ( !bw.CancellationPending )
            {
                Thread.Sleep( sleepPeriod );
                this.pictureBox1.Image = GetImage();
                this.pictureBox1.Width = 621;
                this.pictureBox1.Height = 426;
                this.pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            }

            return result;
        }

        private Image GetImage()
        {
            byte[] imageArray;
            using ( var webClient = new WebClient() )
            {
                imageArray = webClient.DownloadData( "http://10.0.0.53/webcapture.jpg?command=snap&channel=1&user=admin&password=JYvL3uhC" );
            }

            Image image;
            using ( var ms = new MemoryStream( imageArray ) )
            {
                image = Image.FromStream( ms );
            }

            List<Face> faces = MakeAnalysisRequest( imageArray );
            foreach ( var face in faces )
            {
                using ( Graphics g = Graphics.FromImage( image ) )
                {
                    Color customColor = Color.FromArgb( 100, Color.Yellow );
                    Pen pen = new Pen( customColor, 2 );
                    g.DrawRectangle( pen, new Rectangle( 
                        face.FaceRectangle.Left, 
                        face.FaceRectangle.Top,
                        face.FaceRectangle.Width,
                        face.FaceRectangle.Height ) );
                }
            }

            return image;
        }

        const string subscriptionKey = "8ffd363207ad475ca0436d985f9614db";
        const string uriBase = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0/detect";

        private List<Face> MakeAnalysisRequest( byte[] byteData )
        {
            HttpClient client = new HttpClient();

            // Request headers.
            client.DefaultRequestHeaders.Add( "Ocp-Apim-Subscription-Key", subscriptionKey );

            // Request parameters. A third optional parameter is "details".
            string requestParameters = "returnFaceId=true&returnFaceLandmarks=false" +
                "&returnFaceAttributes=age,gender,headPose,smile,facialHair,glasses," +
                "emotion,hair,makeup,occlusion,accessories,blur,exposure,noise";

            // Assemble the URI for the REST API Call.
            string uri = uriBase + "?" + requestParameters;

            HttpResponseMessage response;
            List<Face> faces = new List<Face>();
            using ( ByteArrayContent content = new ByteArrayContent( byteData ) )
            {
                // This example uses content type "application/octet-stream".
                // The other content types you can use are "application/json"
                // and "multipart/form-data".
                content.Headers.ContentType = new MediaTypeHeaderValue( "application/octet-stream" );

                // Execute the REST API call.
                response = client.PostAsync( uri, content ).GetAwaiter().GetResult();

                // Get the JSON response.
                string contentString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                try
                {
                    faces = JsonConvert.DeserializeObject<List<Face>>( contentString );
                } catch ( Exception ) { }
            }

            return faces;
        }

        static byte[] GetImageAsByteArray( string imageUrl )
        {
            using ( var webClient = new WebClient() )
            {
                return webClient.DownloadData( imageUrl );
            }
        }

        private class Face
        {
            [JsonProperty( "faceId" )]
            public string FaceId { get; set; }

            [JsonProperty( "faceRectangle" )]
            public FaceRectangle FaceRectangle { get; set; }
        }

        private class FaceRectangle
        {
            [JsonProperty( "width" )]
            public int Width { get; set; }
            [JsonProperty( "height" )]
            public int Height { get; set; }
            [JsonProperty( "left" )]
            public int Left { get; set; }
            [JsonProperty( "top" )]
            public int Top { get; set; }

        }

    }
}
