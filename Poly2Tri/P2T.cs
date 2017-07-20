namespace Poly2Tri {
	public static class P2T {
		//private static TriangulationAlgorithm _defaultAlgorithm = TriangulationAlgorithm.DTSweep;

        //public static void Triangulate(PolygonSet ps) {
        //    TriangulationContext tcx = CreateContext(_defaultAlgorithm);
        //    foreach (Polygon p in ps.Polygons) {
        //        tcx.PrepareTriangulation(p);
        //        Triangulate(tcx);
        //        tcx.Clear();
        //    }
        //}

        // 2를 변형하여, 1 2 3 4 를 합친다.
        public static void Triangulate(Polygon p)
        {
            TriangulationContext tcx;

            tcx = new DTSweepContext();

            tcx.PrepareTriangulation((Triangulatable)p);

            DTSweep.Triangulate((DTSweepContext)tcx);
            
        }
        
        //// 1
        //// Triangulate를 기본 알고리즘인 DTSweep으로 호출한다.
        //public static void Triangulate(Polygon p) {
        //    Triangulate(_defaultAlgorithm, p);
        //}

        ////public static void Triangulate(ConstrainedPointSet cps) {
        ////    Triangulate(_defaultAlgorithm, cps);
        ////}

        ////public static void Triangulate(PointSet ps) {
        ////    Triangulate(_defaultAlgorithm, ps);
        ////}

        //// 2
        //public static void Triangulate(TriangulationAlgorithm algorithm, Triangulatable t)
        //{
        //    // 
        //    // tcx 변수 생성
        //    TriangulationContext tcx;

        //    //long time = System.nanoTime();
        //    // 기본 알고리즘인 DTSweep으로 tcx를 생성한다.
        //    // DTSweepContext는 TriangulationContext를 상속받는다.
        //    tcx = CreateContext(algorithm);     // 3
        //    // Polygon은 Triangulatable Interface를 상속받고있다.
        //    // tcx로 PrepareTriangulation함수를 호출하여 Triangulation을 준비한다.
        //    tcx.PrepareTriangulation(t);
        //    Triangulate(tcx);                   // 4
        //    //logger.info( "Triangulation of {} points [{}ms]", tcx.getPoints().size(), ( System.nanoTime() - time ) / 1e6 );
        //}

        //// 3
        //public static TriangulationContext CreateContext(TriangulationAlgorithm algorithm) {
        //    switch (algorithm) {
        //    case TriangulationAlgorithm.DTSweep:
        //    default:
        //            // DTSweepContext 초기화
        //        return new DTSweepContext();
        //    }
        //}

        //// 4
        //public static void Triangulate(TriangulationContext tcx) {
        //    switch (tcx.Algorithm) {
        //    case TriangulationAlgorithm.DTSweep:
        //    default:
        //            // tcx를 DTSweepContext로 casting하여 Triangulate함수를 호출한다.
        //        DTSweep.Triangulate((DTSweepContext)tcx);
        //        break;
        //    }
        //}

		/// <summary>
		/// Will do a warmup run to let the JVM optimize the triangulation code -- or would if this were Java --MM
		/// </summary>
		public static void Warmup() {
#if false
			/*
			 * After a method is run 10000 times, the Hotspot compiler will compile
			 * it into native code. Periodically, the Hotspot compiler may recompile
			 * the method. After an unspecified amount of time, then the compilation
			 * system should become quiet.
			 */
			Polygon poly = PolygonGenerator.RandomCircleSweep2(50, 50000);
			TriangulationProcess process = new TriangulationProcess();
			process.triangulate(poly);
#endif
		}
	}
}
