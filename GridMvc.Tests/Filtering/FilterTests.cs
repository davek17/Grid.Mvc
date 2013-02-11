﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using GridMvc.Filtering;
using GridMvc.Sorting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GridMvc.Tests.Filtering
{
    [TestClass]
    public class FilterTests
    {
        private TestGrid _grid;
        private TestRepository _repo;

        [TestInitialize]
        public void Init()
        {
            HttpContext.Current = new HttpContext(
                new HttpRequest("", "http://tempuri.org", ""),
                new HttpResponse(new StringWriter()));

            _repo = new TestRepository();
            _grid = new TestGrid(_repo.GetAll());
        }

        [TestMethod]
        public void TestFilter()
        {
            var mock = new Mock<IGridFilterSettings>();
            mock.Setup(settings => settings.ColumnName).Returns("Created");
            mock.Setup(settings => settings.Type).Returns(GridFilterType.LessThan);
            mock.Setup(settings => settings.Value).Returns("10.05.2005");
            var filter = new DefaultColumnFilter<TestModel, DateTime>(m => m.Created);

            var filtered = filter.ApplyFilter(_repo.GetAll().AsQueryable(), mock.Object);

            var original = _repo.GetAll().AsQueryable().Where(t => t.Created < new DateTime(2005, 5, 10));

            for (int i = 0; i < filtered.Count(); i++)
            {
                if (filtered.ElementAt(i).Id != original.ElementAt(i).Id)
                    Assert.Fail("Filtering not works");
            }

            //var processed processor.Process()
        }

        [TestMethod]
        public void TestFilteringDateTimeLessThan()
        {
            var filterValue = new DateTime(2005, 5, 10);
            var settings = MockFilterSetting("Created", filterValue.Date.ToString(CultureInfo.InvariantCulture), GridFilterType.LessThan);
            TestFiltering(settings, x => x.Created, x => x.Created < filterValue);
        }

        [TestMethod]
        public void TestFilteringDateTimeLessThanWithCustomInternalColumnName()
        {
            var filterValue = new DateTime(2005, 5, 10);
            var settings = MockFilterSetting("someid", filterValue.ToShortDateString(), GridFilterType.LessThan);
            _grid.Columns.Add(x => x.Created, "someid").Filterable(true);
            if (!ValidateFiltering(_grid, settings, x => x.Created < filterValue))
            {
                Assert.Fail("Filtering works incorrect");
            }
        }

        [TestMethod]
        public void TestFilteringStringEquals()
        {
            var firstItem = _repo.GetAll().First();
            var settings = MockFilterSetting("Title", firstItem.Title, GridFilterType.Contains);
            TestFiltering(settings, x => x.Title, x => x.Title.ToUpper() == firstItem.Title.ToUpper());
        }

        [TestMethod]
        public void TestFilteringStringEqualsCaseInsensative()
        {
            var firstItem = _repo.GetAll().First();
            var settings = MockFilterSetting("Title", firstItem.Title.ToUpper(), GridFilterType.Contains);
            TestFiltering(settings, x => x.Title, x => x.Title.ToUpper() == firstItem.Title.ToUpper());
        }

        [TestMethod]
        public void TestFilteringStringContains()
        {
            var firstItem = _repo.GetAll().First();
            var settings = MockFilterSetting("Title", firstItem.Title, GridFilterType.Contains);
            TestFiltering(settings, x => x.Title, x => x.Title.ToUpper().Contains(firstItem.Title.ToUpper()));
        }

        [TestMethod]
        public void TestFilteringIntEquals()
        {
            var firstItem = _repo.GetAll().First();
            var settings = MockFilterSetting("Id", firstItem.Title, GridFilterType.Contains);
            TestFiltering(settings, x => x.Title, x => x.Id == firstItem.Id);
        }

        [TestMethod]
        public void TestFilteringChildEquals()
        {
            var firstItem = _repo.GetAll().First();
            var settings = MockFilterSetting("Created2", firstItem.Child.ChildCreated.Date.ToString(CultureInfo.InvariantCulture), GridFilterType.Equals);
            TestFiltering(settings, x => x.Child.ChildCreated, x => x.Child.ChildCreated == firstItem.Child.ChildCreated);
        }

        private void TestFiltering<T>(IGridFilterSettings settings, Expression<Func<TestModel, T>> column,
                                   Func<TestModel, bool> filterContraint)
        {
            _grid.Columns.Add(column, settings.ColumnName).Filterable(true);
            if (!ValidateFiltering(_grid, settings, filterContraint))
            {
                Assert.Fail("Filtering works incorrect");
            }
        }

        private bool ValidateFiltering(TestGrid grid, IGridFilterSettings filterSettings,
                                                        Func<TestModel, bool> filterExpression)
        {
            var settingsMock = new Mock<IGridSettingsProvider>();
            settingsMock.Setup(s => s.FilterSettings).Returns(filterSettings);
            settingsMock.Setup(s => s.SortSettings).Returns(new QueryStringSortSettings());
            grid.Settings = settingsMock.Object;

            IEnumerable<TestModel> resultCollection = _grid.ItemsToDisplay.OfType<TestModel>();
            if (!resultCollection.Any()) Assert.Fail("No items to compare");

            IEnumerable<TestModel> etalonCollection = _repo.GetAll().Where(filterExpression);

            if (!ValidateCollectionsTheSame(resultCollection, etalonCollection))
            {
                return false;
            }
            return true;
        }

        private IGridFilterSettings MockFilterSetting(string columnName, string filterValue, GridFilterType type)
        {
            var mock = new Mock<IGridFilterSettings>();
            mock.Setup(settings => settings.ColumnName).Returns(columnName);
            mock.Setup(settings => settings.Type).Returns(type);
            mock.Setup(settings => settings.Value).Returns(filterValue);
            mock.Setup(settings => settings.IsInitState).Returns(false);
            return mock.Object;
        }

        private bool ValidateCollectionsTheSame<T>(IEnumerable<T> collection1, IEnumerable<T> collection2)
        {
            for (int i = 0; i < collection1.Count(); i++)
            {
                if (!collection1.ElementAt(i).Equals(collection2.ElementAt(i)))
                    return false;
            }
            return true;
        }

    }
}