using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinHeap<T>
{
    Node_<T> root;
    int id = 0;

    public MinHeap(List<T> data=null, List<float> values=null) {
        if ((data != null) && (values != null)) {
            root = new Node_<T>(data[0], values[0], id, this);
            root.root = true;
            id++;

            for (int i = 1; i < values.Count; i++) {
                AddNode(new Node_<T>(data[i], values[i], id, this));
                id++;
            }
        }
       
    }

    // adds node to heap, when tree is not dead
    public void AddNode(Node_<T> node) {
        if (root == null) {
            root = node;
            root.root = true;
        } else {
            Node_<T> leaf = root.GetLeaf();
            leaf.children.Add(node);
            node.parent = leaf;
            node.Evaporate();
        }
    }

    // returns object of node with minimal value
    public T GetMinObject() {
        return root.obj;
    }

    // returns value of node with minimal value
    public float GetMinValue() {
        return root.value;
    }

    // returns object of node with minimal value and removes it from heap
    public T Pop() {
        T obj = root.obj;
        root.Pop();
        return obj;
    }

    // tree = dead
    public void DestroyRoot() {
        root = null;
    }
}

public class Node_<T> {
    public bool root = false;
    public T obj;
    public float value = 0;
    public int id = -1;
    public List<Node_<T>> children = new List<Node_<T>>();
    public Node_<T> parent;
    public MinHeap<T> mh;

    public Node_(T object_, float value_, int id_, MinHeap<T> mh_) {
        obj = object_;
        value = value_;
        id = id_;
        mh = mh_;
    }

    public void DestroyChild(float id_) {
        for (int i = 0; i < children.Count; i++) {
            if (children[i].id == id_) {children.RemoveAt(i); return;}
        }
    }

    public int FindChildrenWithId(float id_) {
        for (int i = 0; i < children.Count; i++) {
            if (children[i].id == id_) {return i;}
        }
        return -1;
    }

    public void Evaporate() {
        if (!root && parent.value > value) {
            T obj_ = parent.obj;
            parent.obj = obj;
            obj = obj_;

            float val = parent.value;
            parent.value = value;
            value = val;

            parent.Evaporate();
        }
    }

    public void CopyProperties(Node_<T> n) {
        obj = n.obj;
        value = n.value;
    }

    public void Pop() {
        if (children.Count == 0) {
            if (!root) {
                parent.DestroyChild(id); 
            } else {
                mh.DestroyRoot(); 
            }
            
            return;
        }

        int ii = 0;
        if (children.Count > 1) {
            if (children[0].value > children[1].value) {
                ii = 1;
            }
        }

        CopyProperties(children[ii]);
        children[ii].Pop();
    }

    public Node_<T> GetLeaf() {
        if (children.Count < 2) {
            return this;
        }
        return children[Random.Range(0, 2)].GetLeaf();
    }
}

