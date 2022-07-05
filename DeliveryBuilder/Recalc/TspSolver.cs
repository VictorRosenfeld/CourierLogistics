
namespace DeliveryBuilder.Recalc
{
    using DeliveryBuilder.Geo;
    using DeliveryBuilder.Log;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Решение задачи коммивояжера:
    /// построение обхода минимальной длины
    /// </summary>
    public class TspSolver
    {
        /// <summary>
        /// Построитель выпуклой оболочки
        /// </summary>
        private readonly OuelletConvexHullCpp convexHullBuilder;

        /// <summary>
        /// Comparer для сравнеия двух проекций по (TargetEdgeIndex, CoordX)
        /// </summary>
        private readonly CompareProjections comparer;

        /// <summary>
        /// Конструктор класса TspSolver
        /// </summary>
        public TspSolver()
        {
            convexHullBuilder = new OuelletConvexHullCpp();
            comparer = new CompareProjections();
        }

        /// <summary>
        /// Решение задачи коммивояжера:
        /// построение обхода минимальной длины
        /// </summary>
        /// <param name="points">Исходные точки</param>
        /// <param name="pointCount">Количество исходых точек от начала массива points</param>
        /// <param name="tourIndices">Индексы точек построенного обхода</param>
        /// <returns>0 - обход построен; иначе - обход не построен</returns>
        public int Solve(GeoPoint[] points, int pointCount, ref int[] tourIndices)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Поверяем исходные данные
                rc = 2;
                if (points == null || points.Length <= 0)
                    return rc;
                if (pointCount <= 0 || pointCount >= points.Length)
                    return rc;
                if (tourIndices == null || tourIndices.Length < pointCount)
                    return rc;

                // 3. Частные случаи
                rc = 3;
                switch (pointCount)
                {
                    case 1:
                        tourIndices[0] = 0;
                        return rc = 0;
                    case 2:
                        tourIndices[0] = 0;
                        tourIndices[1] = 1;
                        return rc = 0;
                }

                // 4. Точки для дальнейшего использования
                rc = 4;
                PointEx[] pointsEx = new PointEx[pointCount];

                for (int i = 0; i < pointCount; i++)
                {
                    pointsEx[i].index = i;
                    pointsEx[i].x = points[i].Latitude;
                    pointsEx[i].y = points[i].Longitude;
                }

                // 5. Находим выпуклую оболочку для всех исходных точек
                rc = 5;
                PointEx[] сonvexHullPoints = convexHullBuilder.OuelletConvexHull(pointsEx, pointsEx.Length, true);

                // Если в выпуклую оболочку вошли все точки
                if (сonvexHullPoints.Length > pointCount)
                {
                    for (int i = 0; i < pointCount; i++)
                    { tourIndices[i] = сonvexHullPoints[i].index; }
                    return rc = 0;
                }

                // 6. Инициализируем обход
                rc = 6;
                List<TourNodeEx> tour = new List<TourNodeEx>(pointCount + 1);

                for (int i = 0; i < сonvexHullPoints.Length; i++)
                {
                    tour.Add(new TourNodeEx(сonvexHullPoints[i], 0));
                }

                // 7. Цикл построения обхода
                rc = 7;
                PointEx[] levelPoints = new PointEx[pointCount - сonvexHullPoints.Length + 2];
                bool[] isTourPoint = new bool[pointCount];
                int levelPointsCount = 0;
                int tourCount = 0;

                // iterationMode = 0  - построение проекций;
                // iterationMode = 1  - склейка обхода с выпуклой оболочкой;
                int iterationMode = 0;

                for (int level = 1; level < pointCount; level++)
                {
                    // 7.1 Выбираем точки не входящие в обход
                    rc = 71;
                    //Array.Clear(isTourPoint, 0, isTourPoint.Length); // Можно удалить !
                    levelPointsCount = 0;
                    tourCount = tour.Count;

                    for (int i = 0; i < tourCount; i++)
                    { isTourPoint[tour[i].Location.index] = true; }

                    for (int i = 0; i < pointCount; i++)
                    {
                        if (!isTourPoint[i])
                        {
                            levelPoints[levelPointsCount++] = pointsEx[i];
                        }
                    }

                    // 7.2 Если точек больше нет
                    rc = 72;
                    if (levelPointsCount <= 0)
                        break;

                    // 7.3 Строим выпуклую оболочку для точек, не входящих в обход
                    rc = 73;
                    InsertTourNodesEx insertNodes = null;

                    switch (levelPointsCount)
                    {
                        case 1:
                            insertNodes = GetClosestTourEdge(tour, levelPoints[0], -1);
                            if (insertNodes == null)
                            {
                                tour.Insert(1, new TourNodeEx(levelPoints[0], level));
                            }
                            else
                            {
                                AddNodes(tour, insertNodes, level);
                            }
                            continue;
                        case 2:
                            сonvexHullPoints = new PointEx[] { levelPoints[0], levelPoints[1], levelPoints[0] };
                            break;
                        default:
                            сonvexHullPoints = convexHullBuilder.OuelletConvexHull(levelPoints, levelPointsCount, true);
                            break;
                    }

                    // 7.4 Строим проекции точек выпуклой оболочки
                    rc = 74;
                    if (iterationMode == 0)
                    {
                        int count = 0;
                        PointProjectionsEx[] allProjections = new PointProjectionsEx[сonvexHullPoints.Length];
                        PointProjectionsEx projections = new PointProjectionsEx(сonvexHullPoints[0], 0, сonvexHullPoints[сonvexHullPoints.Length - 2], сonvexHullPoints[1]);
                        GetPointProjections(tour, projections, level - 1);
                        allProjections[0] = projections;
                        if (projections.ProjectionCount > 0)
                        { count++; }

                        for (int i = 1; i < сonvexHullPoints.Length - 1; i++)
                        {
                            projections = new PointProjectionsEx(сonvexHullPoints[i], i, сonvexHullPoints[i - 1], сonvexHullPoints[i + 1]);
                            GetPointProjections(tour, projections, level - 1);
                            allProjections[i] = projections;
                            if (projections.ProjectionCount > 0)
                            { count++; }
                        }

                        allProjections[allProjections.Length - 1] = allProjections[0].Clone(allProjections.Length - 1);

                        // 7.5 Если нет ни одной проекции
                        rc = 75;
                        if (count <= 0)
                        {
                            //insertNodes = GetClosestTourEdge(tour, сonvexHullPoints, level - 1);
                            insertNodes = GetClosestTourEdge(tour, сonvexHullPoints, -1);
                            if (insertNodes == null)
                                return rc;
                            AddNodes(tour, insertNodes, level);
                            continue;
                        }

                        // 7.6 Выбираем все построенные проекции и их ключи
                        rc = 76;
                        PointProjectionEx[] projes;
                        long[] projKeys;
                        SelectPointProjectionsWithKeys(allProjections, out projes, out projKeys);

                        Array.Sort(projKeys, projes);

                        // 7.7 Обработка ребер выпуклой оболочки, основания проекций которых лежат на одном ребре обхода
                        rc = 77;
                        PointProjectionEx[] pointTarget = new PointProjectionEx[сonvexHullPoints.Length - 1];
                        int count2 = 0;
                        PointProjectionEx minProj1 = null;
                        PointProjectionEx minProj2 = null;

                        while (true)
                        {
                            long prevKey = -2;
                            PointProjectionEx prevProjection = null;
                            double mind = double.MaxValue;
                            int index1 = -1;
                            int index2 = -1;

                            for (int i = 0; i < projes.Length; i++)
                            {
                                // 7.8 Если точка уже выбрана
                                rc = 78;
                                PointProjectionEx projection = projes[i];
                                int pointIndex = projection.PointIndex;
                                pointIndex = pointIndex % (сonvexHullPoints.Length - 1);
                                if (pointTarget[pointIndex] != null)
                                    continue;

                                // 7.9 Если вместе с предыдущей точкой принадлежат одному ребру
                                rc = 79;
                                long pointKey = projection.Key;

                                if (pointKey - prevKey == 1)
                                {
                                    double d = prevProjection.Distance + projection.Distance;
                                    if (d < mind)
                                    {
                                        mind = d;
                                        index1 = prevProjection.PointIndex;
                                        index2 = pointIndex;
                                        minProj1 = prevProjection;
                                        minProj2 = projection;
                                    }
                                }
                                else
                                {
                                    prevKey = pointKey;
                                    prevProjection = projection;
                                }
                            }

                            // 7.10 Если пара точек не найдена
                            rc = 710;
                            if (index1 < 0)
                                break;

                            pointTarget[index1] = minProj1;
                            pointTarget[index2] = minProj2;
                            count2 += 2;
                        }

                        // 7.11 Обрабатываем одиночные точки
                        rc = 711;
                        int count1 = 0;

                        if (count2 < pointTarget.Length)
                        {
                            while (true)
                            {
                                double mind = double.MaxValue;
                                int index1 = -1;

                                for (int i = 0; i < projes.Length; i++)
                                {
                                    // 7.12 Если точка уже выбрана
                                    rc = 712;
                                    PointProjectionEx projection = projes[i];
                                    int pointIndex = projection.PointIndex;
                                    if (pointIndex >= pointTarget.Length)
                                        continue;
                                    if (pointTarget[pointIndex] != null)
                                        continue;

                                    // 7.13 Отбираем точку с самой короткой проекцией
                                    rc = 713;
                                    if (projection.Distance < mind)
                                    {
                                        mind = projection.Distance;
                                        index1 = pointIndex;
                                        minProj1 = projection;
                                    }
                                }

                                // 7.14 Если проекция не найдена
                                rc = 714;
                                if (index1 < 0)
                                    break;

                                pointTarget[index1] = minProj1;
                                if (++count1 + count2 >= pointTarget.Length)
                                    break;
                            }
                        }

                        // 7.15 Добавляем распределеные вершины в обход
                        rc = 715;
                        count = count1 + count2;

                        if (count > 0)
                        {
                            // 7.16 Выбираем распределенные точки для добавления в обход
                            rc = 716;
                            PointEx[] pts = new PointEx[count];
                            if (count >= pointTarget.Length)
                            {
                                Array.Copy(сonvexHullPoints, pts, pts.Length);
                            }
                            else
                            {
                                count = 0;
                                for (int i = 0; i < pointTarget.Length; i++)
                                {
                                    if (pointTarget[i] != null)
                                    {
                                        pts[count] = сonvexHullPoints[i];
                                        pointTarget[count++] = pointTarget[i];
                                    }
                                }
                            }

                            // 7.17 Сортируем точки в порядке добавления (TargetEdgeIndex, CoordX)
                            rc = 717;
                            Array.Sort(pointTarget, pts, 0, count, comparer);

                            // 7.18 Цикл добавления точек 
                            rc = 718;
                            int startIndex = 0;
                            int endIndex = 0;
                            int offset = 0;
                            int currentEdgeIndex = pointTarget[0].TourEdgeIndex;

                            for (int i = 1; i < count; i++)
                            {
                                PointProjectionEx projection = pointTarget[i];
                                int edgeIndex = projection.TourEdgeIndex;
                                if (edgeIndex == currentEdgeIndex)
                                {
                                    endIndex = i;
                                }
                                else
                                {
                                    // Добавление точек к текущему ребру обхода
                                    int length = endIndex - startIndex + 1;
                                    //PointEx[] addPts = new PointEx[length];
                                    //Array.Copy(pts, startIndex, addPts, 0, length);
                                    //InsertTourNodesEx insertNodes1 = new InsertTourNodesEx(currentEdgeIndex + offset, addPts, 0);
                                    //AddNodes(tour, insertNodes1, level);
                                    AddNodes(tour, pts, startIndex, length, currentEdgeIndex + offset, level);

                                    // Переход к следующему ребру обхода
                                    currentEdgeIndex = edgeIndex;
                                    startIndex = i;
                                    endIndex = i;
                                    offset += length;
                                }
                            }

                            // Обработка последнего ребра
                            int length2 = endIndex - startIndex + 1;
                            //PointEx[] addPts2 = new PointEx[length2];
                            //Array.Copy(pts, startIndex, addPts2, 0, length2);
                            //InsertTourNodesEx insertNodes2 = new InsertTourNodesEx(currentEdgeIndex + offset, addPts2, 0);
                            //AddNodes(tour, insertNodes2, level);
                            AddNodes(tour, pts, startIndex, length2, currentEdgeIndex + offset, level);
                        }

                        if (count == 0)
                        {
                            // 7.19 Ни одна из вершин не рапределена
                            rc = 719;
                            InsertTourNodesEx insertNodes3 = GetClosestTourEdge(tour, сonvexHullPoints, level - 1);
                            if (insertNodes3 != null)
                            {
                                AddNodes(tour, insertNodes3, level);
                            }
                            else
                            {
                                insertNodes3 = GetClosestTourEdge(tour, сonvexHullPoints, -1);
                                if (insertNodes3 == null)
                                    return rc;
                                AddNodes(tour, insertNodes3, level);
                            }
                        }
                        else if (count < pointTarget.Length)
                        {
                            iterationMode = 1;
                        }
                    }
                    else
                    {
                        InsertTourNodesEx insertNodes3 = GetClosestTourEdge(tour, сonvexHullPoints, level - 1);
                        if (insertNodes3 != null)
                        {
                            AddNodes(tour, insertNodes3, level);
                        }
                        else
                        {
                            insertNodes3 = GetClosestTourEdge(tour, сonvexHullPoints, -1);
                            if (insertNodes3 == null)
                                return rc;
                            AddNodes(tour, insertNodes3, level);
                        }

                        iterationMode = 0;
                    }
                }

                // 8. Формируем результат
                rc = 8;
                for (int i = 0; i < tour.Count - 1; i++)
                { tourIndices[i] = tour[i].Location.index; }

                // 9. Выход - Ok
                rc = 0;
                return rc;
            }
            catch (Exception ex)
            {
                Logger.WriteToLog(669, MessageSeverity.Error, string.Format(Messages.MSG_669, $"{nameof(TspSolver)}.{nameof(this.Solve)}", rc, (ex.InnerException == null ? ex.Message : ex.InnerException.Message)));
                return rc;
            }
        }

        /// <summary>
        /// Поиск ближайшего ребра обхода для заданной точки со следующими свойствами:
        /// 1) Обе вершины ребра обхода имеют заданный уровень
        /// </summary>
        /// <param name="tour">Вершины закмкнутого пути</param>
        /// <param name="pointIndex">Индекс тчки в исходном массиве</param>
        /// <param name="point">Исходная точка</param>
        /// <param name="edgeLevel">Заданный уровень вершин ребра. Отрицательное значение - уровень не проверяется</param>
        /// <returns>Найденное ребро или null</returns>
        private static InsertTourNodesEx GetClosestTourEdge(List<TourNodeEx> tour, PointEx point, int edgeLevel)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (tour == null || tour.Count <= 1)
                { return null; }

                // 3. Цикл поиска ребра
                double x = point.x;
                double y = point.y;
                double minDist = double.MaxValue;
                int afterNodeIndex = -1;

                for (int i = 0; i < tour.Count - 1; i++)
                {
                    // 3.1 Провеяем уровень вершин ребра - условие 1)
                    if (edgeLevel >= 0 &&
                        (tour[i].ConvexHullLevel != edgeLevel ||
                        tour[i + 1].ConvexHullLevel != edgeLevel))
                        continue;

                    PointEx pt1 = tour[i].Location;
                    PointEx pt2 = tour[i + 1].Location;
                    if (pt1.x == pt2.x && pt1.y == pt2.y)
                        continue;

                    // 3.2 Выбираем наиболее близкое ребро
                    double dx = pt1.x - x;
                    double dy = pt1.y - y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    dx = pt2.x - x;
                    dy = pt2.y - y;
                    dist += Math.Sqrt(dx * dx + dy * dy);

                    if (dist < minDist)
                    {
                        minDist = dist;
                        afterNodeIndex = i;
                    }
                }

                if (afterNodeIndex < 0)
                    return null;

                // 4. Выход Ok
                return new InsertTourNodesEx(afterNodeIndex, new PointEx[] { point }, minDist);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Поиск ближайшего ребра обхода для заданной пары точек со следующими свойствами:
        /// 1) Обе вершины ребра обхода имеют заданный уровень
        /// </summary>
        /// <param name="tour">Вершины закмкнутого пути</param>
        /// <param name="point1">Исходная точка 1</param>
        /// <param name="point2">Исходная точка 2/param>
        /// <param name="edgeLevel">Заданный уровень вершин ребра. Отрицательное значение - уровень не проверяется</param>
        /// <returns>Найденное ребро или null</returns>
        private static InsertTourNodesEx GetClosestTourEdge(List<TourNodeEx> tour, PointEx point1, PointEx point2, int edgeLevel)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (tour == null || tour.Count <= 1)
                { return null; }

                if (point1.x == point2.x && point1.y == point2.y)
                { return null; }

                // 3. Цикл поиска ребра
                double x1 = point1.x;
                double y1 = point1.y;
                double x2 = point2.x;
                double y2 = point2.y;
                double dx = x1 - x2;
                double dy = y1 - y2;
                double d12 = Math.Sqrt(dx * dx + dy * dy);
                double a = y2 - y1;
                double b = x1 - x2;
                double c = y1 * x2 - x1 * y2;

                double minDist = double.MaxValue;
                int afterNodeIndex = -1;
                PointEx insertPt1 = new PointEx();
                PointEx insertPt2 = new PointEx();

                for (int i = 0; i < tour.Count - 1; i++)
                {
                    // 3.1 Провеяем уровень вершин ребра - условие 1)
                    if (edgeLevel >= 0 &&
                        (tour[i].ConvexHullLevel != edgeLevel ||
                        tour[i + 1].ConvexHullLevel != edgeLevel))
                        continue;

                    // 3.2 Проверяем совпадение координат вершин 
                    PointEx pt1 = tour[i].Location;
                    PointEx pt2 = tour[i + 1].Location;
                    if (pt1.x == pt2.x && pt1.y == pt2.y)
                        continue;

                    // 3.3 Подсчиываем длину ребра обхода
                    double xx1 = pt1.x;
                    double yy1 = pt1.y;
                    double xx2 = pt2.x;
                    double yy2 = pt2.y;
                    if (a * xx1 + b * yy1 + c < 0 ||
                        a * xx2 + b * yy2 + c < 0)
                        continue;

                    dx = xx1 - xx2;
                    dy = yy1 - yy2;
                    double dd12 = Math.Sqrt(dx * dx + dy * dy);

                    // 3.4 Проверяем путь: pt1 -> point1 -> point2 -> pt2
                    dx = xx1 - x1;
                    dy = yy1 - y1;
                    double d1 = Math.Sqrt(dx * dx + dy * dy) + d12 - dd12;
                    dx = xx2 - x2;
                    dy = yy2 - y2;
                    d1 += Math.Sqrt(dx * dx + dy * dy);

                    // 3.5 Проверяем путь: pt1 -> point2 -> point1 -> pt2
                    dx = xx1 - x2;
                    dy = yy1 - y2;
                    double d2 = Math.Sqrt(dx * dx + dy * dy) + d12 - dd12;
                    dx = xx2 - x1;
                    dy = yy2 - y1;
                    d2 += Math.Sqrt(dx * dx + dy * dy);

                    // 3.6 Отбираем наилучший вариант
                    if (d1 <= d2)
                    {
                        if (d1 < minDist)
                        {
                            minDist = d1;
                            afterNodeIndex = i;
                            insertPt1 = point1;
                            insertPt2 = point2;
                        }
                    }
                    else
                    {
                        if (d2 < minDist)
                        {
                            minDist = d2;
                            afterNodeIndex = i;
                            insertPt1 = point2;
                            insertPt2 = point1;
                        }
                    }
                }

                if (afterNodeIndex < 0)
                    return null;

                // 4. Выход Ok
                return new InsertTourNodesEx(afterNodeIndex, new PointEx[] { insertPt1, insertPt2 }, minDist);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Поиск реберной склейки обхода и замкнутого контура
        /// </summary>
        /// <param name="tour">Вершины закмкнутого контура</param>
        /// <param name="closedContour">Замкнутый контур</param>
        /// <param name="edgeLevel">Заданный уровень вершин ребра. Отрицательное значение - уровень не проверяется</param>
        /// <returns>Найденное ребро или null</returns>
        private static InsertTourNodesEx GetClosestTourEdge(List<TourNodeEx> tour, PointEx[] closedContour, int edgeLevel)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (tour == null || tour.Count <= 1)
                { return null; }

                if (closedContour == null || closedContour.Length < 3)
                { return null; }

                // 3. Цикл поиска ребра
                InsertTourNodesEx minDistEdge = null;
                int minIndex = -1;
                int n = closedContour.Length - 1;

                for (int i = 0; i < n; i++)
                {
                    // 3.1 Проверяем очередное ребро
                    InsertTourNodesEx insertNodes = GetClosestTourEdge(tour, closedContour[i], closedContour[i + 1], edgeLevel);

                    // 3.2 Выбираем случай с миималным расстоянием
                    if (insertNodes != null)
                    {
                        if (minDistEdge == null || insertNodes.Distance < minDistEdge.Distance)
                        {
                            minIndex = i;
                            minDistEdge = insertNodes;
                        }
                    }
                }

                if (minIndex >= 0)
                {
                    PointEx[] pts = new PointEx[n];
                    if (minDistEdge.Points[0].x == closedContour[minIndex].x && minDistEdge.Points[0].y == closedContour[minIndex].y)
                    {
                        for (int i = minIndex; i > minIndex - n; i--)
                        {
                            int k = (i >= 0 ? i : n + i);
                            pts[minIndex - i] = closedContour[k];
                        }
                    }
                    else
                    {
                        for (int i = minIndex; i < minIndex + n; i++)
                        {
                            pts[i - minIndex] = closedContour[i % n];
                        }
                    }

                    minDistEdge = new InsertTourNodesEx(minDistEdge.AfterNodeIndex, pts, minDistEdge.Distance);
                }


                // 4. Выход...
                return minDistEdge;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Добавляение новых вершин в обход
        /// </summary>
        /// <param name="tour">Обход</param>
        /// <param name="insertPoints">Добавляемые вершины</param>
        /// <param name="level">Уровень добавляемых вершин</param>
        /// <returns>true - вершины добавлены; false - вершины не добавлены</returns>
        private static bool AddNodes(List<TourNodeEx> tour, InsertTourNodesEx insertPoints, int level)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (tour == null || tour.Count <= 0)
                    return false;
                if (insertPoints == null || insertPoints.PointCount <= 0)
                    return false;
                int afterIndex = insertPoints.AfterNodeIndex;
                if (afterIndex < 0 || afterIndex >= tour.Count)
                    return false;

                // 3. Создаём добавляемые элементы
                TourNodeEx[] addNodes = new TourNodeEx[insertPoints.PointCount];

                for (int i = 0; i < addNodes.Length; i++)
                {
                    addNodes[i] = new TourNodeEx(insertPoints.Points[i], level);
                }

                // 4. Добавляем элементы
                tour.InsertRange(afterIndex + 1, addNodes);

                // 5. Выхд - Ok
                return true;
            }
            catch
            { return false; }
        }

        /// <summary>
        /// Вставка новых вершин в обход
        /// </summary>
        /// <param name="tour">Обход</param>
        /// <param name="points">Массив с добавляемыми точками</param>
        /// <param name="startIndex">Индекс первой добавляемой точки</param>
        /// <param name="pointCount">Количество добавляемых точек</param>
        /// <param name="afterIndex">Индекс вершины обхода, после которой вставляются точки</param>
        /// <param name="level">Уровень добавляемых вершин</param>
        /// <returns>true - вершины вставлены; false - вершины не вставлены</returns>
        private static bool AddNodes(List<TourNodeEx> tour, PointEx[] points, int startIndex, int pointCount, int afterIndex, int level)
        {
            // 1. Инициализация

            try
            {
                // 2. Проверяем исходные данные
                if (tour == null || tour.Count <= 0)
                    return false;
                if (points == null || points.Length <= 0)
                    return false;
                if (startIndex < 0)
                    return false;
                if (pointCount <= 0 || startIndex + pointCount > points.Length)
                    return false;
                if (afterIndex < 0 || afterIndex >= tour.Count)
                    return false;

                // 3. Создаём добавляемые элементы
                TourNodeEx[] addNodes = new TourNodeEx[pointCount];

                for (int i = startIndex; i < startIndex + pointCount; i++)
                {
                    addNodes[i] = new TourNodeEx(points[i], level);
                }

                // 4. Вставляем элементы
                tour.InsertRange(afterIndex + 1, addNodes);

                // 5. Выход - Ok
                return true;
            }
            catch
            { return false; }
        }

        /// <summary>
        /// Поиск проекций на ребра обхода со следующими свойствами:
        /// 1) Обе вершины рабра обхода имеют заданный уровень
        /// 2) Проекция заданной точки лежит на ребре
        /// 3) Проекция точки лежит вне выпуклой оболочки, которой она принадлежит
        /// </summary>
        /// <param name="tour">Вершины закмкнутого пути</param>
        /// <param name="pointProjections">Проекции точки</param>
        /// <param name="edgeLevel">Заданный уровень вершин ребра. Отрицательное значение - уровень не проверяется</param>
        /// <returns>0 - проекции построены; иначе - проекции не построены</returns>
        private static int GetPointProjections(List<TourNodeEx> tour, PointProjectionsEx pointProjections, int edgeLevel)
        {
            // 1. Инициализация
            int rc = 1;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (tour == null || tour.Count <= 1)
                { return rc; }
                if (pointProjections == null)
                    return rc;
                //if (edgeLevel < 0)
                //{ return rc; }

                // 3. Цикл поиска ребра
                rc = 3;
                PointEx point = pointProjections.Location;
                double x = point.x;
                double y = point.y;
                double alpha;
                PointEx previousLocation = pointProjections.PreviousLocation;
                PointEx nextLocation = pointProjections.NextLocation;
                double a = nextLocation.y - previousLocation.y;
                double b = previousLocation.x - nextLocation.x;
                double c = previousLocation.y * nextLocation.x - previousLocation.x * nextLocation.y;
                //int pointSign = Math.Sign(a * x + b * y + c);
                //Console.WriteLine($"edge_level = {edgeLevel}, Point = ({x}, {y}), Sign = {pointSign}");

                for (int i = 0; i < tour.Count - 1; i++)
                {
                    // 3.3 Провеяем уровень вершин ребра - условие 1)
                    rc = 33;
                    if (edgeLevel >= 0 &&
                        (tour[i].ConvexHullLevel != edgeLevel ||
                        tour[i + 1].ConvexHullLevel != edgeLevel))
                        continue;

                    PointEx pt1 = tour[i].Location;
                    PointEx pt2 = tour[i + 1].Location;
                    if (pt1.x == pt2.x && pt1.y == pt2.y)
                        continue;

                    // 3.4 Находим alpha, для которого достигается минимум
                    //    (1) min dist(alpha * pt1 + (1 - alpha) * pt2, point),
                    // где
                    //     pt1 = tour[i].Location;
                    //     pt2 = tour[i + 1].Location;
                    // Приравниваем в (1) призводную по alpha нулю и находим alpha
                    //
                    //               (x - x2) * (x1 - x2) + (y - y2) * (y1 - y2) 
                    //      alpha = --------------------------------------------- , 
                    //                       (x1 - x2)² + (y1 - y2)²
                    // где
                    //       x = point.X,
                    //       y = point.Y,
                    //       x1 = pt1.X,
                    //       y1 = pt1.Y,
                    //       x2 = pt2.X,
                    //       y2 = pt2.Y,
                    rc = 34;
                    double x1 = pt1.x;
                    double y1 = pt1.y;
                    double x2 = pt2.x;
                    double y2 = pt2.y;
                    double dx12 = x1 - x2;
                    double dy12 = y1 - y2;
                    double d2 = dx12 * dx12 + dy12 * dy12;

                    alpha = ((x - x2) * dx12 + (y - y2) * dy12) / d2;
                    if (alpha < 0 || alpha > 1)  // условие 2)
                        continue;

                    double alpha1 = (1.0 - alpha);
                    double coordX = alpha1 * Math.Sqrt(d2);

                    // 3.5 Проверяем условие 3 и сохраняем проекцию 
                    rc = 35;
                    double x0 = alpha * x1 + alpha1 * x2;
                    double y0 = alpha * y1 + alpha1 * y2;
                    //if (Math.Sign(a * x0 + b * y0 + c) >= 0)
                    //if (Math.Sign(a * x0 + b * y0 + c) == pointSign)
                    if (a * x0 + b * y0 + c >= 0)
                    {
                        dx12 = x - x0;
                        dy12 = y - y0;

                        pointProjections.AddProjection(i, Math.Sqrt(dx12 * dx12 + dy12 * dy12), coordX);
                    }
                }

                if (pointProjections.ProjectionCount <= 0)
                    return rc;

                // 4. Выход Ok
                rc = 0;
                return rc;
            }
            catch
            {
                return rc;
            }
        }

        /// <summary>
        /// Выбор всех проекций и их ключей
        /// </summary>
        /// <param name="allProjections">Все проекции</param>
        /// <param name="projections">Выбранные проекции</param>
        /// <param name="keys">Ключи выбранных проекций</param>
        /// <returns>0 - проекции и ключи выбраны; иначе - проекции и ключи не выбраны</returns>
        private static int SelectPointProjectionsWithKeys(PointProjectionsEx[] allProjections, out PointProjectionEx[] projections, out long[] keys)
        {
            // 1. Инициализация
            int rc = 1;
            projections = null;
            keys = null;

            try
            {
                // 2. Проверяем исходные данные
                rc = 2;
                if (allProjections == null || allProjections.Length <= 0)
                    return rc;

                // 3. Подсчитываем общее число проекций
                rc = 3;
                int count = 0;
                for (int i = 0; i < allProjections.Length; i++)
                { count += allProjections[i].ProjectionCount; }

                if (count <= 0)
                    return rc;

                // 4. Выбиаем проекции и их ключи
                rc = 4;
                projections = new PointProjectionEx[count];
                count = 0;

                for (int i = 0; i < allProjections.Length; i++)
                {
                    PointProjectionsEx pointProjections = allProjections[i];
                    int projectionCount = pointProjections.ProjectionCount;
                    if (projectionCount > 0)
                    {
                        Array.Copy(pointProjections.Projections, 0, projections, count, projectionCount);
                        count += projectionCount;
                    }
                }

                // 4. Выбиаем ключи проекций
                rc = 4;
                keys = new long[projections.Length];

                for (int i = 0; i < projections.Length; i++)
                {
                    keys[i] = projections[i].Key;
                }

                // 5. Выход - Ok
                rc = 0;
                return rc;
            }
            catch
            { return rc; }
        }
    }

    /// <summary>
    /// Вершина обхода
    /// </summary>
    internal struct TourNodeEx
    {
        /// <summary>
        /// Вершина
        /// </summary>
        public PointEx Location { get; private set; }

        /// <summary>
        /// Уровень выпуклой оболочки вершину 
        /// </summary>
        public int ConvexHullLevel { get; private set; }

        /// <summary>
        /// Параметрический конструктор структуры TourNodeEx
        /// </summary>
        /// <param name="location">Вершина</param>
        /// <param name="level">Уровень выпуклой оболочки (0-based)</param>
        public TourNodeEx(PointEx location, int level)
        {
            Location = location;
            ConvexHullLevel = level;
        }

        /// <summary>
        /// Перегруженный метод ToString()
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{ConvexHullLevel} - {GetPointId(Location)} = ({Location.x}, {Location.y})";
        }

        /// <summary>
        /// Идеификатор точки с заданными координатами
        /// (for debug only)
        /// </summary>
        /// <param name="pt">Координаты точки</param>
        /// <returns>Идентификатор точки</returns>
        private static string GetPointId(PointEx pt)
        {
            int x = (int)pt.x;
            int y = (int)pt.y;
            string id = "";
            if (x == 9 && y == 7)
            { id = "a1"; }
            else if (x == 8 && y == 8)
            { id = "b1"; }
            else if (x == 9 && y == 6)
            { id = "b2"; }
            else if (x == 11 && y == 7)
            { id = "b3"; }
            else if (x == 6 && y == 6)
            { id = "c1"; }
            else if (x == 8 && y == 5)
            { id = "c2"; }
            else if (x == 12 && y == 6)
            { id = "c3"; }
            else if (x == 12 && y == 9)
            { id = "c4"; }
            else if (x == 9 && y == 10)
            { id = "c5"; }
            else if (x == 6 && y == 8)
            { id = "c6"; }
            else if (x == 4 && y == 6)
            { id = "d1"; }
            else if (x == 7 && y == 3)
            { id = "d2"; }
            else if (x == 13 && y == 4)
            { id = "d3"; }
            else if (x == 15 && y == 9)
            { id = "d4"; }
            else if (x == 13 && y == 12)
            { id = "d5"; }
            else if (x == 7 && y == 12)
            { id = "d6"; }
            else if (x == 5 && y == 9)
            { id = "d7"; }
            else if (x == 2 && y == 2)
            { id = "e1"; }
            else if (x == 10 && y == 1)
            { id = "e2"; }
            else if (x == 16 && y == 3)
            { id = "e3"; }
            else if (x == 19 && y == 7)
            { id = "e4"; }
            else if (x == 18 && y == 13)
            { id = "e5"; }
            else if (x == 13 && y == 17)
            { id = "e6"; }
            else if (x == 5 && y == 15)
            { id = "e7"; }
            else if (x == 3 && y == 10)
            { id = "e8"; }

            return id;
        }
    }

    /// <summary>
    /// Данные для вставки подряд идущих точек в обход
    /// </summary>
    internal class InsertTourNodesEx
    {
        /// <summary>
        /// Индекс вершины обхода после которой следует вставить новые вершины
        /// </summary>
        public int AfterNodeIndex { get; private set; }

        /// <summary>
        /// Добавляемые точки
        /// </summary>
        public PointEx[] Points { get; private set; }

        /// <summary>
        /// Расстояние
        /// </summary>
        public double Distance { get; private set; }

        /// <summary>
        /// Количество добавляемых точек
        /// </summary>
        public int PointCount => (Points == null ? 0 : Points.Length);

        /// <summary>
        /// Парамерический констуктор класса InsertTourNodesEx
        /// </summary>
        /// <param name="afterNodeIndex">Индекс вершины обхода, после которой добавляются новые вершины</param>
        /// <param name="points">Координаты добавляемых точек</param>
        /// <param name="distance">Расстояние</param>
        public InsertTourNodesEx(int afterNodeIndex, PointEx[] points, double distance)
        {
            AfterNodeIndex = afterNodeIndex;
            Points = points;
            Distance = distance;
        }
    }

    /// <summary>
    /// Проекции вершины выпуклой оболочки
    /// </summary>
    internal class PointProjectionsEx
    {
        /// <summary>
        /// Исходная вершина
        /// </summary>
        public PointEx Location { get; private set; }

        /// <summary>
        /// Индекс исходной вершины в выпуклой оболочке
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// Предшествующая вершина в выпуклой оболочке
        /// </summary>
        public PointEx PreviousLocation { get; private set; }

        /// <summary>
        /// Следующая вершина в выпуклой оболочке
        /// </summary>
        public PointEx NextLocation { get; private set; }

        /// <summary>
        /// Проекции вершины
        /// </summary>
        private PointProjectionEx[] projections;

        /// <summary>
        /// Проекции вершины
        /// </summary>
        public PointProjectionEx[] Projections => projections;

        /// <summary>
        /// Количество проекций
        /// </summary>
        public int ProjectionCount => (projections == null ? 0 : projections.Length);

        /// <summary>
        /// Добавление проекции
        /// </summary>
        /// <param name="projection"></param>
        public void AddProjection(PointProjectionEx projection)
        {
            if (projection == null)
                return;

            Array.Resize(ref projections, projections.Length + 1);
            projections[projections.Length - 1] = projection;
        }

        /// <summary>
        /// Добавление проекции
        /// </summary>
        /// <param name="projection"></param>
        public void AddProjection(int tourNodeIndex, double distance, double coordx)
        {
            Array.Resize(ref projections, projections.Length + 1);
            projections[projections.Length - 1] = new PointProjectionEx(tourNodeIndex, Index, distance, coordx);
        }

        /// <summary>
        /// Параметрический конструктор класса PointProjectionsEx
        /// </summary>
        /// <param name="location">Координаты исходной вершины</param>
        /// <param name="index">Индекс исходной вершины в выпуклой оболочке</param>
        /// <param name="previousLocation">Координаты предшествующей вершины выпуклой оболочки</param>
        /// <param name="nextLocation">Координаты следующей вершины выпуклой оболочки</param>
        public PointProjectionsEx(PointEx location, int index, PointEx previousLocation, PointEx nextLocation)
        {
            Location = location;
            Index = index;
            PreviousLocation = previousLocation;
            NextLocation = nextLocation;
            projections = new PointProjectionEx[0];
        }

        /// <summary>
        /// Создание клона с заменой индекса точки
        /// </summary>
        /// <param name="indexReplacement">Устанавливаемый индекс точки</param>
        /// <returns></returns>
        public PointProjectionsEx Clone(int indexReplacement)
        {
            PointProjectionsEx projectionsClone = new PointProjectionsEx(Location, indexReplacement, PreviousLocation, NextLocation);
            foreach (var projection in projections)
            {
                projectionsClone.AddProjection(projection.TourEdgeIndex, projection.Distance, projection.CoordX);
            }

            return projectionsClone;
        }
    }

    /// <summary>
    /// Проекция вершины
    /// </summary>
    internal class PointProjectionEx
    {
        /// <summary>
        /// Индекс ребра обхода - основания проекции
        /// (иднекс первой его вершины)
        /// </summary>
        public int TourEdgeIndex { get; private set; }

        /// <summary>
        /// Индекс проецируемой точки в выпуклой оболочке 
        /// </summary>
        public int PointIndex { get; private set; }

        /// <summary>
        /// Длина проекции
        /// </summary>
        public double Distance { get; private set; }

        /// <summary>
        /// Расстояние от основания проекции
        /// до первой вершины целевого ребра в обходе
        /// </summary>
        public double CoordX { get; private set; }

        /// <summary>
        /// Ключ проекции
        /// </summary>
        public long Key => GetKey(TourEdgeIndex, PointIndex);

        /// <summary>
        /// Построение ключа для пары (edgeIndex, pointIndex)
        /// </summary>
        /// <param name="edgeIndex">Индекс ребра обхода - основания проекции</param>
        /// <param name="pointIndex">Индекс проецируемой точки</param>
        /// <returns>Ключ</returns>
        public static long GetKey(int edgeIndex, int pointIndex)
        {
            return (long)edgeIndex << 32 | pointIndex;
        }

        /// <summary>
        /// Параметрический констуктор класса PointProjectionEx
        /// </summary>
        /// <param name="edgeIndex">Индекс ребра обхода - основания проекции</param>
        /// <param name="pointIndex">Индекс проецируемй точки</param>
        /// <param name="distance">Длина проекции</param>
        /// <param name="coordX">Расстояние от основания проекции до первой вершины целевого ребра в обходе</param>
        public PointProjectionEx(int edgeIndex, int pointIndex, double distance, double coordX)
        {
            TourEdgeIndex = edgeIndex;
            PointIndex = pointIndex;
            Distance = distance;
            CoordX = coordX;
        }
    }

    /// <summary>
    /// Comparer для сравнения PointProjectionEx при сортировке
    /// </summary>
    internal class CompareProjections : IComparer<PointProjectionEx>
    {
        public int Compare(PointProjectionEx p1, PointProjectionEx p2)
        {
            if (p1.TourEdgeIndex < p2.TourEdgeIndex)
                return -1;
            if (p1.TourEdgeIndex > p2.TourEdgeIndex)
                return 1;
            if (p1.CoordX < p2.CoordX)
                return -1;
            if (p1.CoordX > p2.CoordX)
                return 1;
            return 0;
        }
    }
}
