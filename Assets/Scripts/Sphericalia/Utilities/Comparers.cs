using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Circle0Comparer : IComparer<SphCircle> // -1 y>x      1 x>y
{
    public int Compare(SphCircle x, SphCircle y)
    {
        if (x.layer == y.layer) {
            if (x.Static == y.Static) {return 0;}
            else if (!x.Static) {return 1;}
            else {return -1;}
        } else if (x.layer > y.layer) {return 1;} else {return -1;}
    }
}

public class Gon0Comparer : IComparer<SphGon> // -1 y>x      1 x>y
{
    public int Compare(SphGon x, SphGon y)
    {
        if (x.layer == y.layer) {
            if (x.Static == y.Static) {return 0;}
            else if (!x.Static) {return 1;}
            else {return -1;}
        } else if (x.layer > y.layer) {return 1;} else {return -1;}
    }
}

public class Shape0Comparer : IComparer<SphShape> // -1 y>x      1 x>y
{
    public int Compare(SphShape x, SphShape y)
    {
        if (x.layer == y.layer) {
            if (x.Static == y.Static) {return 0;}
            else if (!x.Static) {return 1;}
            else {return -1;}
        } else if (x.layer > y.layer) {return 1;} else {return -1;}
    }
}

// second series
public class Circle1Comparer : IComparer<SphCircle> // -1 y>x      1 x>y
{
    public int Compare(SphCircle x, SphCircle y)
    {
        if (x.Static == y.Static) {
            if (x.layer == y.layer) {return 0;}
            else if (x.layer > y.layer) {return 1;}
            else {return -1;}
        } else if (!x.Static) {return 1;} else {return -1;}
    }
}

public class Gon1Comparer : IComparer<SphGon> // -1 y>x      1 x>y
{
    public int Compare(SphGon x, SphGon y)
    {
        if (x.Static == y.Static) {
            if (x.layer == y.layer) {return 0;}
            else if (x.layer > y.layer) {return 1;}
            else {return -1;}
        } else if (!x.Static) {return 1;} else {return -1;}
    }
}

public class Shape1Comparer : IComparer<SphShape> // -1 y>x      1 x>y
{
    public int Compare(SphShape x, SphShape y)
    {
        if (x.Static == y.Static) {
            if (x.layer == y.layer) {return 0;}
            else if (x.layer > y.layer) {return 1;}
            else {return -1;}
        } else if (!x.Static) {return 1;} else {return -1;}
    }
}

public class LLayerComparer : IComparer<PointLight> // -1 y>x      1 x>y
{
    public int Compare(PointLight x, PointLight y)
    {
        if (x.layer == y.layer) {return 0;}
        else if (x.layer > y.layer) {return 1;} else {return -1;}
    }
}