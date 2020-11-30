using System;
using System.Linq;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace ServerTest
{
    class WorldGenerator
    {
        readonly static string MAPPath = @"C:\Users\djque\Desktop\Visual Studio Projects\C#\ServerTest\ServerTest\MAP.bmp";
        //Ocean color used
        public static Color ocean = Color.FromArgb(29, 162, 216);
        //Landmass color used
        public static Color land = Color.FromArgb(126, 200, 80);

        static Random random = new Random();

        static List<Circle[]> generations = new List<Circle[]>();

        public static Bitmap D2LandMap;

        public class Circle
        {
            public Circle parent;

            public Circle[] children;
            public bool childrenDefined;

            public List<Vector2> perimeter;
            public bool perimeterDefined;

            public List<Vector2> area;
            public bool areaDefined;

            public Vector2 center;

            public float resolutionFactor;
            public float radius;

            public Circle(Vector2 _center, float _radius, float _resolutionFactor, Circle _parent, int numChildren)
            {
                perimeter = new List<Vector2>();
                area = new List<Vector2>();

                children = new Circle[numChildren];

                center = _center;
                radius = _radius;
                resolutionFactor = 1 / _resolutionFactor;
                parent = _parent;

                areaDefined = false;
                perimeterDefined = false;
                childrenDefined = false;
            }
           
            public List<Vector2> RemoveDuplicates(List<Vector2> original)
            {
                List<Vector2> newList = new List<Vector2>();
                #region Method 1
                /*try
                {
                    newList.Exists(query => query.Equals(original[0]));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception thrown attempting to remove duplicates: {ex}");
                    return original;
                }
                foreach (Vector2 testObj in original)
                {
                    if (!newList.Exists(query => query.Equals(testObj)))
                    {
                        newList.Add(testObj);
                    }
                }*/
                #endregion
                #region Method 2
                newList = original.Distinct().ToList();
                #endregion
                return newList;
            }

            public void DrawPerimeter(Bitmap bmp, Color color)
            {
                foreach (Vector2 perimeter in perimeter)
                {
                    int currentX = (int)MathF.Max(perimeter.X, 0);
                    int currentY = (int)MathF.Max(perimeter.Y, 0);
                    bmp.SetPixel(currentX, currentY, color);
                }
            }

            public void DrawArea(Bitmap bmp, Color color)
            {
                foreach (Vector2 area in area)
                {
                    int currentX = (int)MathF.Max(area.X, 0);
                    int currentY = (int)MathF.Max(area.Y, 0);
                    bmp.SetPixel(currentX, currentY, color);
                }
            }

            public static void DiffCircles(Circle original, Circle toSubtract)
            {
                #region Method 1
                /*
                for (int i = original.area.Count - 1; i > 0; i--)
                {
                    for (int j = toSubtract.area.Count - 1; j > 0; j--)
                    {
                        if (original.area[i - 1].X == toSubtract.area[j - 1].X
                            && original.area[i - 1].Y == toSubtract.area[j - 1].Y)
                        {
                            original.area.Remove(original.area[i - 1]);
                        }
                    }

                }*/

                #endregion
                #region Method 2
                /*for (int i = 0; i < toSubtract.area.Count; i++)
                {
                    bool matchFound = false;
                    Vector2 match = original.area.Find(query => matchFound = query.Equals(toSubtract.area[i]));
                    if (matchFound)
                    {
                        original.area.Remove(match);
                    }
                }*/
                #endregion
                #region Method 3
                List<Vector2> newArea = original.area.Except(toSubtract.area).ToList();
                original.area = newArea;
                #endregion
            }

            public void RedefinePerimeter()
            {
                Vector2 xBounds = GetXBounds(area);
                Vector2 yBounds = GetYBounds(area);

                List<Vector2> newPerimeter = new List<Vector2>();
                List<Vector2> yLevel = new List<Vector2>();
                List<Vector2> xLevel = new List<Vector2>();
                for (int i = (int)yBounds.X; i < (int)yBounds.Y; i++)
                {
                    yLevel.Clear();
                    foreach (Vector2 XV2 in area)
                    {
                        if ((int)XV2.Y == i)
                        {
                            yLevel.Add(XV2);
                        }
                    }
                    if (yLevel.Count > 0)
                    {
                        Vector2 XBoundsByY = GetXBounds(yLevel);
                        int minX = (int)XBoundsByY.X;
                        int maxX = (int)XBoundsByY.Y;
                        Vector2 minXV = new Vector2(minX, i);
                        Vector2 maxXV = new Vector2(maxX, i);
                        newPerimeter.Add(yLevel.Find(x => x.Equals(minXV)));
                        newPerimeter.Add(yLevel.Find(x => x.Equals(maxXV)));
                    }
                }
                for (int i = (int)xBounds.X; i < (int)xBounds.Y; i++)
                {
                    xLevel.Clear();
                    foreach (Vector2 YV2 in area)
                    {
                        if ((int)YV2.X == i)
                        {
                            xLevel.Add(YV2);
                        }
                    }
                    if (yLevel.Count > 0)
                    {
                        Vector2 YBoundsByX = GetXBounds(yLevel);
                        int minY = (int)YBoundsByX.X;
                        int maxY = (int)YBoundsByX.Y;
                        Vector2 minYV = new Vector2(minY, i);
                        Vector2 maxYV = new Vector2(maxY, i);
                        newPerimeter.Add(xLevel.Find(x => x.Equals(minYV)));
                        newPerimeter.Add(xLevel.Find(x => x.Equals(maxYV)));
                    }
                }
                perimeter = newPerimeter;
                perimeterDefined = true;
            }

            public void DefinePerimeter()
            {
                for (float i = 0; i < 370; i += resolutionFactor)
                {
                    Vector2 surfPt = new Vector2(
                        (int)(center.X + radius * MathF.Cos(i)),
                        (int)(center.Y + radius * MathF.Sin(i)));
                    perimeter.Add(surfPt);
                }
                perimeter = RemoveDuplicates(perimeter);
                perimeterDefined = true;
            }

            public void DefineArea()
            {
                Vector2 yBounds = GetYBounds(perimeter);
                int minY = (int)yBounds.X;
                int maxY = (int)yBounds.Y;
                if (perimeterDefined == true)
                {
                    List<Vector2> yLevel = new List<Vector2>();
                    for (int i = minY; i < maxY; i++)
                    {
                        yLevel.Clear();
                        foreach (Vector2 XV2 in perimeter)
                        {
                            if ((int)XV2.Y == i)
                            {
                                yLevel.Add(XV2);
                            }
                        }
                        if (yLevel.Count > 0)
                        {
                            Vector2 xBounds = GetXBounds(yLevel);
                            int minX = (int)xBounds.X;
                            int maxX = (int)xBounds.Y;
                            for (int j = minX; j < maxX; j++)
                            {
                                area.Add(new Vector2(j, i));
                            }
                        }
                    }
                    areaDefined = true;
                }
                else
                {
                    Console.WriteLine("Error: Perimeter not defined!");
                }
            }

            public List<Vector2> CalculateIntersection()
            {
                List<Vector2> perimeterIntersection = new List<Vector2>();
                #region Method 1
                /*
                //for(int i = 0; i < perimeter.Count; i ++)
                //{
                //    if (generations[0][0].area.Exists(query => query.Equals(perimeter[i])))
                //    {
                //        perimeterIntersection.Add(perimeter[i]);
                //    }
                //}
                // P1(Center of child circle): (x + r1 * cosB, y + r1 * sinB)
                // P2(Perimeter - Perimeter Intersection 1): (x + r1 * cosA, y + r1 * sinA)
                // P3(Perimeter - Perimeter Intersection 2): (x + r1 * cosA, y - r1 * sinA)
                //alpha is acos(1 - (r2 ^ 2 / (2r1 ^ 2))
                */
                #endregion

                #region Method 2
                /*
                Vector2 parentCenter = parent.center;
                float parentRadius = parent.radius;
                float alpha = MathF.Acos(1 - (MathF.Pow(radius, 2) / (2 * MathF.Pow(parentRadius, 2))));
                float sinAlpha = MathF.Sin(alpha);
                float cosAlpha = MathF.Cos(alpha);

                Vector2 P2 = new Vector2((int)(parentCenter.X + parentRadius * cosAlpha), (int)(parentCenter.Y + parentRadius * sinAlpha));
                Vector2 P3 = new Vector2((int)(parentCenter.X + parentRadius * cosAlpha), (int)(parentCenter.Y - parentRadius * sinAlpha));


                //Use Law of Cosines to find angle between P1,P2,P3
                //Then iterate over 360-gamma to get the exterior perimeter
                float P2P3Dist = Vector2.Distance(P2, P3);
                float rad = Vector2.Distance(P2, center);
                float gamma = MathF.Acos(MathF.Pow(P2P3Dist, 2) / (2 * radius * (radius - 1)));
                float P0P2Dist = Vector2.Distance(new Vector2(center.X + radius, center.Y), P2);
                float offset = MathF.Acos(MathF.Pow(P0P2Dist, 2) / (2 * radius * (radius - 1)));

                for (int i = 0; i < 360-gamma; i++)
                {
                    Vector2 surfPt = new Vector2(
                        (int)(center.X + radius * MathF.Cos(i+offset)),
                        (int)(center.Y + radius * MathF.Sin(i+offset)));
                    perimeterIntersection.Add(surfPt);
                }*/
                #endregion

                #region Method 3
                perimeterIntersection = perimeter.Intersect(generations[0][0].area).ToList();
                #endregion
                return perimeterIntersection;
            }
        }

        /// <summary>GetYBounds takes a List<Vector2> range and returns a Vector2(min,max),
        /// the min and max of the Y values of that range</summary>
        /// <param name="range"></param>
        /// <returns>The range to determine the bounds for</returns>
        public static Vector2 GetYBounds(List<Vector2> range)
        {
            int max = (int)range[0].Y;
            int min = (int)range[0].Y;

            int newMax;
            int newMin;

            foreach (Vector2 v2 in range)
            {
                newMax = (int)v2.Y;
                newMin = (int)v2.Y;

                if (newMax > max)
                {
                    max = newMax;
                }
                if (newMin < min)
                {
                    min = newMin;
                }
            }
            return new Vector2(min, max);
        }

        /// <summary>GetXBounds takes a List<Vector2> range and returns a Vector2(min,max),
        /// the min and max of the X values of that range</summary>
        /// <param name="range"></param>
        /// <returns>The range to determine the bounds for</returns>
        public static Vector2 GetXBounds(List<Vector2> range)
        {
            int max = (int)range[0].X;
            int min = (int)range[0].X;

            int newMax;
            int newMin;

            foreach (Vector2 v2 in range)
            {
                newMax = (int)v2.X;
                newMin = (int)v2.X;

                if (newMax > max)
                {
                    max = newMax;
                }
                if (newMin < min)
                {
                    min = newMin;
                }
            }
            return new Vector2(min, max);
        }

        public static void DrawLine(List<Vector2> points, Bitmap bmp, Color color)
        {
            foreach (Vector2 point in points)
            {
                int currentX = (int)MathF.Max(point.X, 0);
                int currentY = (int)MathF.Max(point.Y, 0);
                bmp.SetPixel(currentX, currentY, color);
            }
        }

        public static void Generate2DMap(int width, int height, Color color)
        {
            /*INITIALIZER STEP
            Starts by creating a blank map with a Color color background.*/
            D2LandMap = new Bitmap(width, height);
            for (int i = 0; i < D2LandMap.Width; i++)
            {
                for (int j = 0; j < D2LandMap.Height; j++)
                {
                    D2LandMap.SetPixel(i, j, color);
                }
            }
        }

        public static void GenerateAllAreas()
        {
            DateTime startTime = DateTime.Now;
            Console.WriteLine("Starting area definition...");
            for (int i = 0; i < generations.Count; i++)
            {
                for (int j = 0; j < generations[i].Length; j++)
                {
                    if (!generations[i][j].areaDefined)
                    {
                        Circle circle = generations[i][j];
                        Action genArea = new Action(() => 
                        { 
                            circle.DefineArea();
                        });
                        Task newTask = Task.Factory.StartNew(genArea);
                    }
                }
            }
            DateTime endTime = DateTime.Now;
            float time = (float)endTime.Subtract(startTime).TotalSeconds;
            Console.WriteLine($"Area definition completed in {time} s");
        }

        public static void DifferentiateAllAreas()
        {
            DateTime totalStartTime = DateTime.Now;
            for (int i = 1; i < generations.Count - 1; i++)
            {
                for (int j = 0; j < generations[i].Length; j++)
                {
                    DateTime startTime = DateTime.Now;
                    Circle.DiffCircles(generations[0][0], generations[i][j]);
                    DateTime endTime = DateTime.Now;
                    float time = (float)endTime.Subtract(startTime).TotalSeconds;
                    Console.WriteLine($"Differentiated area of circle: i:{i} j:{j} in {time} s");
                }
                generations[i] = null;
            }
            DateTime totalEndTime = DateTime.Now;
            float totalTime = (float)totalEndTime.Subtract(totalStartTime).TotalSeconds;
            Console.WriteLine($"All differentiation completed in {totalTime} s");
        }

        public static bool CheckAllAreasDone()
        {
            bool allAreasDone = false;

            bool badLoop = false;
            for (int i = 0; i < generations.Count - 1; i++)
            {
                for (int j = 0; j < generations[i].Length; j++)
                {
                    if (generations[i][j].areaDefined == false)
                    {
                        allAreasDone = false;
                        badLoop = true;
                    }
                }
            }
            if (!badLoop)
            {
                allAreasDone = true;
            }

            return allAreasDone;
        }

        public static void SeedLandmass(Vector2 position, int radius, int numGen, int circlesPerGen, int numSeeds)
        {
            #region Array Initialization
            for (int i = 0; i < numGen; i++)
            {
                Circle[] circles = new Circle[(int)Math.Max(MathF.Pow(circlesPerGen, i), 1)];
                generations.Add(circles);
            }
            #endregion

            /*GENERATE FIRST CIRCLE STEP
            Draw a circle to the map created
            A higher resolution factor increases the resolution of the map
            A low resolution may result in generation artefacts at large radii
            A higher resolution factor may will likely increase overall processing time substantially.*/

            generations[0][0] = new Circle(position, radius, 10, null, circlesPerGen);
            generations[0][0].parent = generations[0][0];
            generations[0][0].DefinePerimeter();
            Console.WriteLine("Defining area of primary circle...");
            generations[0][0].DefineArea();

            /*GENERATION ITERATION STEP
            In this step we are going to iteratively draw circles on the surface of the preceding circle,
            then take the difference of the (i-1)th generation circle with the ith generation circle*/
            //TODO: Add multithreading to process faster

            //A point x,y on a circle is defined to be 
            //x=a+r*sin(q)
            //y=b+r*cos(q)
            //where q is angle, r is radius of circle, and (a,b) is the center of the circle

            #region Perimeter Intersection Definition
            /*PERIMETER INTERSECTION:
             * Defined by 4 points, where:
             * x,y is center of parent circle
             * r1 is parent radius
             * r2 is child radius
             * alpha is the angle between the line connecting the centers of the two circles, 
             *      and the line to the points where their perimeters intersect. +-alpha
             * beta is the angle which the child sits on the parent perimeter
             * P1(Center of child circle): (x+r1*cosB,y+r1*sinB)
             * P2(Perimeter-Perimeter Intersection 1): (x+r1*cosA,y+r1*sinA)
             * P3(Perimeter-Perimeter Intersection 2): (x+r1*cosA,y-r1*sinA)
             * P4(Child circle perimeter point closest to parent center): (x+(r1-r2)*cosB,y+(r1-r2)*sinB)
             * alpha is acos(1-(r2^2/(2r1^2))
             * P4 is *maybe* unnecessary for definition of perimeter intersection
             * Use Law of Cosines to find angle between P1,P2,P3
             * Then iterate over 360-gamma to get the exterior perimeter, which is the perimeter of the circle
             * in the primary circle
             */

            #endregion


            #region Prototype 1
            //TODO: Create first generation at fixed intervals around base circle to ensure the entire circle
            //gets reduced, rather than leaving one side smooth

            //for (int j = 0; j < generations[0].Length; j++)
            //{
            //    Circle precedingCircle = generations[0][j];

            //    for (int k = 0; k < precedingCircle.children.Length; k++)
            //    {
            //        //Thread CreateCircle = new Thread(() => { 

            //        //});
            //        //CreateCircle.Start();
            //        //Tertiary for loop which creates circles and assigns them to generation list arrays and
            //        //parent's children array

            //        float angle = 360 * k / (circlesPerGen - 1);
            //        Vector2 periPos = new Vector2(
            //            precedingCircle.center.X + (precedingCircle.radius) * MathF.Sin(angle),
            //            precedingCircle.center.Y + (precedingCircle.radius) * MathF.Cos(angle));

            //        //Generate circle
            //        Circle toAdd =
            //            new Circle(
            //                periPos,
            //                precedingCircle.radius / 2,
            //                1 / precedingCircle.resolutionFactor,
            //                precedingCircle,
            //                circlesPerGen);

            //        toAdd.DefinePerimeter();
            //        toAdd.DefineArea();

            //        //Add circle to precedingCircle.children
            //        precedingCircle.children[k] = toAdd;
            //        generations[1][k] = toAdd;
            //        if (k == precedingCircle.children.Length - 1)
            //        {
            //            precedingCircle.childrenDefined = true;
            //        }
            //        //toAdd.DrawPerimeter(returnMap, Color.White);
            //        Console.WriteLine($"Creation of circle: i:1, j:{j}, k:{k}");
            //    }
            //}

            //for (int i = 1; i < numGen - 1; i++)
            //{
            //    //Primary for loop which selects which Generation (List<Circle[]>) we are selecting from
            //    //gPos holds the place of which a circle is added to the generation array, so that
            //    //each secondary for loop can appropriately add to the array. is reset in the primary loop
            //    int gPos = 0;

            //    for (int j = 0; j < generations[i].Length; j++)
            //    {
            //        //Secondary for loop which selects the parent circle from the preceding generation
            //        Circle precedingCircle = generations[i][j];

            //            for (int k = 0; k < precedingCircle.children.Length; k++)
            //            {
            //                //Tertiary for loop which creates circles and assigns them to generation list arrays and
            //                //parent's children array
            //                //List<Vector2> perimeterIntersection = new List<Vector2>();
            //                //foreach (Vector2 perim in precedingCircle.perimeter)
            //                //{
            //                //    if (generations[0][0].area.Exists(query => query.Equals(perim)))
            //                //    {
            //                //        perimeterIntersection.Add(perim);
            //                //    }
            //                //}


            //                //Generate circle
            //                Circle toAdd =
            //                    new Circle(
            //                        //perimeterIntersection[random.Next(0, perimeterIntersection.Count - 1)],
            //                        precedingCircle.perimeter[random.Next(0,precedingCircle.perimeter.Count)],
            //                        precedingCircle.radius / 3,
            //                        1 / precedingCircle.resolutionFactor,
            //                        precedingCircle,
            //                        circlesPerGen);

            //                toAdd.DefinePerimeter();
            //                toAdd.DefineArea();

            //                //Add circle to precedingCircle.children
            //                precedingCircle.children[k] = toAdd;

            //                generations[i + 1][gPos++] = toAdd;

            //                Console.WriteLine($"Creation of circle: i:{i + 1}, j:{j}, k:{k}");
            //            }
            //    }
            //}
            //for (int i = 0; i < generations.Count - 1; i++)
            //{
            //    for (int j = 0; j < generations[i].Length; j++)
            //    {
            //        Console.WriteLine($"Differentiating children of circle: i:{i}, j:{j}");
            //        foreach (Circle circle in generations[i][j].children)
            //        {
            //            Circle.DiffCircles(generations[0][0], circle);
            //        }
            //    }
            //}
            //generations[0][0].DrawArea(D2LandMap, land);

            //generations[0][0].RedefinePerimeter();
            #endregion


            #region Prototype 2
            //Declare children of primary circle, so they approriately consume all of the outer perimeter
            //to ensure no excessively circular edges remain
            for (int j = 0; j < generations[0].Length; j++)
            {
                Circle precedingCircle = generations[0][j];

                for (int k = 0; k < precedingCircle.children.Length; k++)
                {
                    float angle = 360 * k / (circlesPerGen - 1);
                    Vector2 periPos = new Vector2(
                        precedingCircle.center.X + (precedingCircle.radius) * MathF.Sin(angle),
                        precedingCircle.center.Y + (precedingCircle.radius) * MathF.Cos(angle));

                    Circle toAdd =
                        new Circle(
                            periPos,
                            precedingCircle.radius / 2,
                            1 / precedingCircle.resolutionFactor,
                            precedingCircle,
                            precedingCircle.children.Length);
                    toAdd.DefinePerimeter();

                    generations[1][k]           = toAdd;
                    precedingCircle.children[k] = toAdd;
                }
            }

            //CONSIDER:
            //Creating subtasks that generate children circles and assign them from within parent circle

            //Loop through and create perimeters of each successive generation of circles
            for (int i = 1; i < generations.Count-1; i++)
            {
                int gPos = 0;

                Console.WriteLine($"Defining perimeters of circles of generation {i + 1}...");
                DateTime startTime = DateTime.Now;
                for (int j = 0; j < generations[i].Length; j++)
                {
                    Circle precedingCircle = generations[i][j];
                    List<Task> tasks = new List<Task>();
                    //List<Vector2> perimeterIntersection = precedingCircle.CalculateIntersection();
                    for(int k = 0; k < precedingCircle.children.Length; k++)
                    {
                        //Action defineCircle = new Action(() => 
                        //{
                            Circle parent = precedingCircle;
                            Circle toAdd =
                                new Circle(
                                    parent.perimeter[random.Next(0, parent.perimeter.Count-1)],
                                    //perimeterIntersection[random.Next(0, perimeterIntersection.Count)],
                                    parent.radius/3,
                                1 / parent.resolutionFactor,
                                    parent,
                                    parent.children.Length
                                    );
                            toAdd.DefinePerimeter();

                            parent.children[k] = toAdd;
                            generations[i + 1][gPos++]  = toAdd;
                        Console.WriteLine($"Circle i{i}, j{j} completed");
                        //});
                        //Task newTask = Task.Factory.StartNew(defineCircle);
                        //tasks.Add(newTask);
                    }
                    Task.WaitAll(tasks.ToArray());
                }
                DateTime stopTime = DateTime.Now;
                float time = (float)stopTime.Subtract(startTime).TotalSeconds;
                Console.WriteLine($"Perimeters of generation {i+1} completed in {time} s.");
            }

            //Generate areas of every circle
            GenerateAllAreas();

            //TODO: Add a wait until all circles are done calculating
            bool allAreasComplete = false;
            while (!allAreasComplete)
            {
                allAreasComplete = CheckAllAreasDone();
            }

            //Calculate difference of generational circles from primary circle
            DifferentiateAllAreas();


            generations[0][0].DrawArea(D2LandMap, land);

            #endregion

            /*Possible Prototype 3 workflow:
             * Create circle (empty, position only)
             * Define circle perimeter
             * Create children objects (empty), assign to array
             * Define area
             * Delete perimeter
             * Subtract area from primary circle
             * Delete circle
             * Repeat for children
             * Loop through each of the primary circles children and do this for each
             * 
             * Possible issues:
             * -Changing area at runtime affects where new circles can be created at, could cause issues
            */
            FileStream mapPath = File.Open(MAPPath, FileMode.OpenOrCreate);
            D2LandMap.Save(mapPath, ImageFormat.Bmp);
            mapPath.Close();
        }
    }
}
