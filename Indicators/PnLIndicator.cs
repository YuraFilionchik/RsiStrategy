using DevExpress.DirectX.Common.Direct2D;
using DevExpress.Text.Interop;
using DevExpress.Xpf.Charts;
using Ecng.Common;
using Ecng.Drawing;
using Ecng.Serialization;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Statistics;
using StockSharp.Localization;
using StockSharp.Messages;
using System.ComponentModel;
using System.Drawing;

/// <summary>
/// P&L indicator that displays profit and loss as histogram bars
/// </summary>
public class PnLIndicator : BaseIndicator
{
    public new bool IsFormed => true;

    protected override IIndicatorValue OnProcess(IIndicatorValue input)
    {
        return input;
    }

    protected override bool CalcIsFormed()
    {
        return true ;
    }

    //protected override Color? Color
    //{

    //    // get => _color;
    //    // set => _color = value ?? System.Drawing.Color.Green;
    //}

}

// Alternative approach using a custom indicator
public class HorizontalLineIndicator : BaseIndicator
{
    private readonly decimal _value;

    public HorizontalLineIndicator(decimal value)
    {
        _value = value;
    }

    protected override bool CalcIsFormed()
    {
        return true;
    }

    protected override IIndicatorValue OnProcess(IIndicatorValue input)
    {
        return input;
    }
    
    public IIndicatorValue Process(DateTimeOffset time )
    {
        return new DecimalIndicatorValue(this, _value, time);
    }
}
//Guid IIndicator.Id => new Guid("PnL");

//string IIndicator.Name { get => _name; set => _name = value; }

//int IIndicator.NumValuesToInitialize => throw new NotImplementedException();

//IIndicatorContainer IIndicator.Container => throw new NotImplementedException();

//IndicatorMeasures IIndicator.Measure => throw new NotImplementedException();

//DrawStyles IIndicator.Style => DrawStyles.Histogram;

//Color? IIndicator.Color => _color;

//event Action<IIndicatorValue, IIndicatorValue> IIndicator.Changed
//{
//    add
//    {
//        throw new NotImplementedException();
//    }

//    remove
//    {
//        throw new NotImplementedException();
//    }
//}

//event Action IIndicator.Reseted
//{
//    add
//    {
//        throw new NotImplementedException();
//    }

//    remove
//    {
//        throw new NotImplementedException();
//    }
//}

////protected override IIndicatorValue OnProcess(IIndicatorValue input)
////{
////    // Just pass through the input value
////    return input;
////}

//IIndicator ICloneable<IIndicator>.Clone()
//{
//    throw new NotImplementedException();
//}

//object ICloneable.Clone()
//{
//    throw new NotImplementedException();
//}

//IIndicatorValue IIndicator.CreateValue(DateTimeOffset time, object[] values)
//{
//    throw new NotImplementedException();
//}

//void IPersistable.Load(SettingsStorage storage)
//{
//    throw new NotImplementedException();
//}

//IIndicatorValue IIndicator.Process(IIndicatorValue input)
//{
//    return input;
//}

//decimal Process(decimal input)
//{
//    return input;
//}

//protected IIndicatorValue OnProcess(IIndicatorValue input)
//{
//    // Just pass through the input value
//    return input;
//}

//void IIndicator.Reset()
//{
//    throw new NotImplementedException();
//}

//void IPersistable.Save(SettingsStorage storage)
//{
//    throw new NotImplementedException();
//}
