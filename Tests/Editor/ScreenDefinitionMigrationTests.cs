using NUnit.Framework;
using UnityEngine;

namespace ScreenSystem.Tests
{
    public class ScreenDefinitionMigrationTests
    {
        // Older assets stored the flags in a nested Behaviors list. The current fields must pick them up.
        [Test]
        public void LegacyBehaviorsAreCopiedToTheFields()
        {
            const string legacyJson = "{\"ScreenName\":\"Splash\",\"ScreenId\":\"Splash\",\"Behaviors\":[{\"KeepVisibleAfterNavigate\":false,\"KeepRaycastEnabledWhenNotTopScreen\":false,\"PersistOverScenes\":true,\"IgnoreNavigationRules\":true}]}";
            ScreenDefinition definition = ScriptableObject.CreateInstance<ScreenDefinition>();

            try
            {
                JsonUtility.FromJsonOverwrite(legacyJson, definition);

                Assert.IsTrue(definition.PersistOverScenes);
                Assert.IsTrue(definition.IgnoreNavigationRules);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void CurrentFieldsAreKeptWithoutLegacyData()
        {
            const string json = "{\"ScreenName\":\"Shop\",\"ScreenId\":\"Shop\",\"PersistOverScenes\":true,\"IgnoreNavigationRules\":false}";
            ScreenDefinition definition = ScriptableObject.CreateInstance<ScreenDefinition>();

            try
            {
                JsonUtility.FromJsonOverwrite(json, definition);

                Assert.IsTrue(definition.PersistOverScenes);
                Assert.IsFalse(definition.IgnoreNavigationRules);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }
    }
}
