using System;
using ColonyFlow.Gameplay;
using NUnit.Framework;
using UnityEngine;

public class MapJsonTests
{
    private const string ValidJson = "{\"rows\":2,\"columns\":2," +
        "\"cameraPosition\":{\"x\":0,\"y\":12,\"z\":-6}," +
        "\"cameraRotation\":{\"x\":60,\"y\":0,\"z\":0}," +
        "\"firstCellPosition\":{\"x\":-2,\"y\":1,\"z\":3}," +
        "\"cellSpacing\":{\"x\":2,\"z\":3}," +
        "\"palette\":[{\"id\":1,\"hex\":\"#E85D75\"},{\"id\":2,\"hex\":\"#4DA8DA80\"}]," +
        "\"cells\":[0,1,2,1]}";

    [Test]
    public void LoadPreservesEmptyCellAndCalculatesWorldPositions()
    {
        var map = MapJsonLoader.Load(ValidJson);
        Assert.AreEqual(4, map.CellCount);
        Assert.AreEqual(3, map.ColoredCellCount);
        Assert.IsTrue(map.GetCell(0, 0).IsEmpty);
        Assert.AreEqual(new Vector3(-2, 1, 3), map.GetCell(0, 0).Position);
        Assert.AreEqual(new Vector3(0, 1, 0), map.GetCell(1, 1).Position);
        Assert.AreEqual(2, map.GetCell(1, 0).ColorId);
        Assert.AreEqual(128f / 255f, map.GetColor(2).a, 0.001f);
    }

    [TestCase("", "empty")]
    [TestCase("{}", "rows")]
    [TestCase("not json", "JSON")]
    public void RejectsInvalidDocument(string json, string message)
    {
        var error = Assert.Throws<FormatException>(() => MapJsonLoader.Load(json));
        StringAssert.Contains(message, error.Message);
    }

    [TestCase("\"cells\":[0,1,2,1]", "\"cells\":[1]", "cells")]
    [TestCase("\"cells\":[0,1,2,1]", "\"cells\":[0,1,9,1]", "cells[2]")]
    [TestCase("\"cells\":[0,1,2,1]", "\"cells\":[0,1,-1,1]", "cells[2]")]
    [TestCase("\"id\":2", "\"id\":1", "palette")]
    [TestCase("#E85D75", "E85D75", "hex")]
    [TestCase("#E85D75", "#GGGGGG", "hex")]
    [TestCase("\"cellSpacing\":{\"x\":2,\"z\":3}", "\"cellSpacing\":{\"x\":0,\"z\":3}", "cellSpacing")]
    [TestCase("\"cameraRotation\":{\"x\":60,\"y\":0,\"z\":0}", "\"cameraRotation\":null", "cameraRotation")]
    public void RejectsInvalidMapFields(string oldValue, string newValue, string message)
    {
        var error = Assert.Throws<FormatException>(() => MapJsonLoader.Load(ValidJson.Replace(oldValue, newValue)));
        StringAssert.Contains(message, error.Message);
    }

    [Test]
    public void ReloadIsTransactionalAndClearLeavesUnrelatedObjects()
    {
        var owner = new GameObject("Map test");
        var cameraObject = new GameObject("Camera test", typeof(Camera));
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var unrelated = new GameObject("Unrelated");
        unrelated.transform.SetParent(owner.transform);
        try
        {
            var view = owner.AddComponent<MapView>();
            view.Configure(prefab, cameraObject.GetComponent<Camera>(), owner.transform);
            view.LoadJson(ValidJson);
            Assert.AreEqual(3, view.SpawnedCellCount);
            Assert.AreEqual(new Vector3(0, 12, -6), cameraObject.transform.position);
            Assert.AreEqual(Quaternion.Euler(60, 0, 0), cameraObject.transform.rotation);
            var previousMap = view.Model;
            Assert.Throws<FormatException>(() => view.LoadJson("{}"));
            Assert.AreSame(previousMap, view.Model);
            Assert.AreEqual(3, view.SpawnedCellCount);
            view.LoadJson(ValidJson);
            Assert.AreEqual(2, owner.transform.childCount); // unrelated + map root
            view.Clear();
            Assert.AreEqual(0, view.SpawnedCellCount);
            Assert.IsNull(view.Model);
            Assert.AreEqual(1, owner.transform.childCount);
            Assert.IsNotNull(unrelated);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(owner);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }
}
