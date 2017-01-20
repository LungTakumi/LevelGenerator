﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum GizmoPreviewState { HIDDEN, ONSELECTION, ALWAYS }
public enum PropertyType { INSTANTIATING, TRANSFORMING, MESHGENERATION };

abstract public class AbstractProperty : MonoBehaviour {
	private bool isDirty = false;
	private ICollection<GameObject> generatedObjects = new List<GameObject> ();
	private GizmoPreviewState previewState = GizmoPreviewState.ONSELECTION;

	//Dirty flag used for components with delayed removal.
	//Helps the generator to never execute a component twice
	public bool IsDirty { get { return isDirty; } set { isDirty = value; } }
	public abstract float ExecutionOrder { get; }
	//If true, the component only gets removed at the end of the generator process
	//After execution the dirty flag will be set to true
	public abstract bool DelayRemoval { get; }
	//Auto Property storing any Objects generated during generation process
	//Null if it is unused by the inherited component
	public ICollection<GameObject> GeneratedObjects { get{ return generatedObjects; } }

	public abstract void Preview();

	public abstract void Generate();

	public AbstractBounds AbstractBounds{
		get { return GetComponentInParent<AbstractBounds> (); }
	}
	public AbstractBounds ParentsAbstractBounds{
		get { return transform.parent.GetComponentInParent<AbstractBounds> (); }
	}

	public MeshFilter PreviewMesh{
		get{
			MeshFilter meshFilter = GetComponent<MeshFilter> ();
			if (meshFilter == null) {
				WildcardAsset wildcard = gameObject.GetComponent<WildcardAsset> ();
				if (wildcard != null) {
					meshFilter = wildcard.IndexedPreviewMesh;
				}
			}
			if (meshFilter == null) {
				meshFilter = gameObject.GetComponentInChildren<MeshFilter> ();
			}
			return meshFilter;
		}
	}

	public GizmoPreviewState GizmoPreviewState{
		get{ return previewState; }
		set{ previewState = value; }
	}

	public virtual void DrawEditorGizmos (){
	}

	void OnDrawGizmos(){
		if (previewState == GizmoPreviewState.ALWAYS) {
			DrawEditorGizmos ();
		}
	}

	void OnDrawGizmosSelected(){
		if (previewState == GizmoPreviewState.ONSELECTION) {
			DrawEditorGizmos ();
		}
	}

	//Should be called before using the PreviewMesh AutoProp to avoid nullref
	public bool MeshFound(){
		return PreviewMesh != null && PreviewMesh.sharedMesh != null;
	}
}

abstract public class ValueProperty : AbstractProperty{
	public override float ExecutionOrder{
		get { return 1; }
	}

	public override bool DelayRemoval{
		get { return false; }
	}
}

abstract public class InstantiatingProperty : AbstractProperty{
	public override float ExecutionOrder{
		get { return 3; }
	}

	public override bool DelayRemoval{
		get { return false; }
	}
}

abstract public class TransformingProperty : AbstractProperty{
	private AbstractBounds abstractBounds;

	public override float ExecutionOrder{
		get { return 2; }
	}

	public override bool DelayRemoval{
		get { return false; }
	}
}

[DisallowMultipleComponent]
abstract public class DoorProperty : AbstractProperty{
	public override float ExecutionOrder{
		get { return 3.9f; }
	}

	public override bool DelayRemoval{
		get { return true; }
	}
}

[DisallowMultipleComponent]
abstract public class MeshProperty : AbstractProperty{
	public override float ExecutionOrder{
		get { return 4; }
	}

	public override bool DelayRemoval{
		get { return false; }
	}
}

[DisallowMultipleComponent]
abstract public class MultiplyingProperty : InstantiatingProperty{
	
	protected abstract Vector3[] CalculatePositions ();

	public override void Generate(){
		Vector3[] copyPositions = CalculatePositions ();

		transform.position = copyPositions [0];

		for (int i = 1; i < copyPositions.Length; i++) {
			GameObject copy = GameObject.Instantiate (gameObject);
			//Array needs to be removed, since the copies are not under control of the Generator yet to handle removal
			DestroyImmediate(copy.GetComponent<MultiplyingProperty> ());
			copy.transform.position = copyPositions [i];
			copy.transform.SetParent (gameObject.transform.parent);
			GeneratedObjects.Add (copy);
		}
	}
}