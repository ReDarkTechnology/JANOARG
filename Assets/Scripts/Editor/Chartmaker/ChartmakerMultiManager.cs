using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;

public class ChartmakerMultiManager
{
    public Type target;

    public List<FieldInfo> AvailableFields;

    public int CurrentFieldIndex;

    public ChartmakerMultiHandler Handler;

    public Dictionary<Type, ChartmakerMultiHandler> Handlers = new Dictionary<Type, ChartmakerMultiHandler>();

    public ChartmakerMultiManager(Type type)
    {
        AvailableFields = new List<FieldInfo>();

        foreach (FieldInfo field in type.GetFields()) 
        {
            if (typeof(IEnumerable).IsAssignableFrom(field.FieldType)
                || typeof(Storyboard) == field.FieldType
                || field.IsStatic || field.IsLiteral || !field.IsPublic) 
            {
                continue;
            }
            AvailableFields.Add(field);
        }

        target = type;
        SetTarget(0);
    }

    public void SetTarget(int target)
    {
        CurrentFieldIndex = target;

        FieldInfo currentField = AvailableFields[target];

        IList current = Chartmaker.current.TargetTimestamp.Count > 1 
            ? Chartmaker.current.TargetTimestamp 
            : Chartmaker.current.TargetThing as IList;

        if (currentField.FieldType != Handler?.TargetType)
        {
            if (currentField.FieldType ==  typeof(bool)) 
            {
                Handler = Handlers.ContainsKey(currentField.FieldType) 
                    ? Handlers[currentField.FieldType] 
                    : new ChartmakerMultiHandlerBoolean();
            }
            else if (currentField.FieldType == typeof(float)) 
            {
                ChartmakerMultiHandlerFloat handler = Handlers.ContainsKey(currentField.FieldType)
                    ? Handlers[currentField.FieldType] as ChartmakerMultiHandlerFloat 
                    : new ChartmakerMultiHandlerFloat();
                handler.SetLerp(current);
                Handler = handler;
            }
            else if (currentField.FieldType == typeof(Vector2)) 
            {
                ChartmakerMultiHandlerVector2 handler = Handlers.ContainsKey(currentField.FieldType)
                    ? Handlers[currentField.FieldType] as ChartmakerMultiHandlerVector2 
                    : new ChartmakerMultiHandlerVector2();
                handler.SetLerp(current);
                Handler = handler;
            }
            else if (currentField.FieldType == typeof(Vector3)) 
            {
                ChartmakerMultiHandlerVector3 handler = Handlers.ContainsKey(currentField.FieldType)
                    ? Handlers[currentField.FieldType] as ChartmakerMultiHandlerVector3 
                    : new ChartmakerMultiHandlerVector3();
                handler.SetLerp(current);
                Handler = handler;
            }
            else 
            {
                Handler = Handlers.ContainsKey(currentField.FieldType) 
                    ? Handlers[currentField.FieldType] 
                    : Activator.CreateInstance(typeof(ChartmakerMultiHandler<>).MakeGenericType(currentField.FieldType)) as ChartmakerMultiHandler;
            }
        }
        Handlers[currentField.FieldType] = Handler;
    }

    public void Execute(IList items, ChartmakerHistory history) {
        FieldInfo currentField = AvailableFields[CurrentFieldIndex];

        ChartmakerMultiEditAction action = new ChartmakerMultiEditAction() 
        { 
            Keyword = currentField.Name 
        };

        foreach(object obj in items) {
            ChartmakerMultiEditActionItem item = new ChartmakerMultiEditActionItem
            {
                Target = obj,
                From = currentField.GetValue(obj),
            };
            item.To = Handler.Get(item.From, obj);
            action.Targets.Add(item);
        }
        action.Redo();
        history.ActionsBehind.Push(action);
        history.ActionsAhead.Clear();
    }
}

public class ChartmakerMultiHandler
{
    public object To;
    
    public virtual object Get(object from, object src) {
        return To;
    }

    public virtual Type TargetType { get; }

}

public class ChartmakerMultiHandler<T>: ChartmakerMultiHandler
{
    
    public override object Get(object from, object src) {
        return Get((T)from, src);
    }
    
    public virtual T Get(T from, object src) {
        return (T)To;
    }
    
    public override Type TargetType { get { return typeof(T); } }
}

public class ChartmakerMultiHandlerBoolean: ChartmakerMultiHandler<bool>
{
    public new bool? To;
    
    public override bool Get(bool from, object src) {
        return To == null ? !from : (bool)To;
    }
}

public class ChartmakerMultiHandlerFloat: ChartmakerMultiHandler<float>
{
    public float From = float.NaN;
    public new float To;

    public FloatOperation Operation;

    public string LerpSource = "Offset";
    FieldInfo LerpField;
    public EaseFunction LerpEasing = EaseFunction.Linear;
    public EaseMode LerpEaseMode = EaseMode.In;

    public float LerpFrom;
    public float LerpTo;

    public void SetLerp(IList list)
    {
        LerpFrom = float.PositiveInfinity;
        LerpTo = float.NegativeInfinity;
        LerpField = list.GetType().GetGenericArguments()[0].GetField(LerpSource);
        if (LerpField == null) return;
        foreach (object item in list)
        {
            float value = (float)LerpField.GetValue(item);
            LerpFrom = Mathf.Min(LerpFrom, value);
            LerpTo = Mathf.Max(LerpTo, value);
        }
    }

    public override float Get(float from, object src) {
        float to = LerpField == null ? LerpTo : Mathf.InverseLerp(LerpFrom, LerpTo, (float)LerpField.GetValue(src));
        to = float.IsFinite(From) ? Mathf.Lerp(From, To, Ease.Get(to, LerpEasing, LerpEaseMode)) : To;
        return FloatOperations[Operation](from, to);
    }

    public enum FloatOperation {
        Set, Add, Multiply, Min, Max, Mirror
    }

    public static Dictionary<FloatOperation, Func<float, float, float>> FloatOperations = new Dictionary<FloatOperation, Func<float, float, float>> {
        { FloatOperation.Set,        (from, to) => to },
        { FloatOperation.Add,        (from, to) => from + to },
        { FloatOperation.Multiply,   (from, to) => from * to },
        { FloatOperation.Min,        (from, to) => Mathf.Min(from, to) },
        { FloatOperation.Max,        (from, to) => Mathf.Max(from, to) },
        { FloatOperation.Mirror,     (from, to) => to - (from - to) },
    };
}


public class ChartmakerMultiHandlerVector2: ChartmakerMultiHandler<Vector2>
{
    public int Axis = 0;
    public float From = float.NaN;
    public new float To;

    public ChartmakerMultiHandlerFloat.FloatOperation Operation;

    public string LerpSource = "Offset";
    FieldInfo LerpField;
    public EaseFunction LerpEasing = EaseFunction.Linear;
    public EaseMode LerpEaseMode = EaseMode.In;

    public float LerpFrom;
    public float LerpTo;

    public void SetLerp(IList list)
    {
        LerpFrom = float.PositiveInfinity;
        LerpTo = float.NegativeInfinity;
        LerpField = list.GetType().GetGenericArguments()[0].GetField(LerpSource);
        if (LerpField == null) return;
        foreach (object item in list)
        {
            float value = (float)LerpField.GetValue(item);
            LerpFrom = Mathf.Min(LerpFrom, value);
            LerpTo = Mathf.Max(LerpTo, value);
        }
    }

    public override Vector2 Get(Vector2 from, object src) {
        float to = LerpField == null ? LerpTo : Mathf.InverseLerp(LerpFrom, LerpTo, (float)LerpField.GetValue(src));
        to = float.IsFinite(From) ? Mathf.Lerp(From, To, Ease.Get(to, LerpEasing, LerpEaseMode)) : To;
        from = new Vector2(from.x, from.y);
        from[Axis] = ChartmakerMultiHandlerFloat.FloatOperations[Operation](from[Axis], to);
        return from;
    }
}

public class ChartmakerMultiHandlerVector3: ChartmakerMultiHandler<Vector3>
{
    public int Axis = 0;
    public float From = float.NaN;
    public new float To;

    public ChartmakerMultiHandlerFloat.FloatOperation Operation;

    public string LerpSource = "Offset";
    FieldInfo LerpField;
    public EaseFunction LerpEasing = EaseFunction.Linear;
    public EaseMode LerpEaseMode = EaseMode.In;

    public float LerpFrom;
    public float LerpTo;

    public void SetLerp(IList list)
    {
        LerpFrom = float.PositiveInfinity;
        LerpTo = float.NegativeInfinity;
        LerpField = list.GetType().GetGenericArguments()[0].GetField(LerpSource);
        if (LerpField == null) return;
        foreach (object item in list)
        {
            float value = (float)LerpField.GetValue(item);
            LerpFrom = Mathf.Min(LerpFrom, value);
            LerpTo = Mathf.Max(LerpTo, value);
        }
    }

    public override Vector3 Get(Vector3 from, object src) {
        float to = LerpField == null ? LerpTo : Mathf.InverseLerp(LerpFrom, LerpTo, (float)LerpField.GetValue(src));
        to = float.IsFinite(From) ? Mathf.Lerp(From, To, Ease.Get(to, LerpEasing, LerpEaseMode)) : To;
        from = new Vector3(from.x, from.y, from.z);
        from[Axis] = ChartmakerMultiHandlerFloat.FloatOperations[Operation](from[Axis], to);
        return from;
    }
}