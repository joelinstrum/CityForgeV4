using System.Collections;using UnityEngine;namespace CityForge.World {public sealed class MountainTestRunner:MonoBehaviour {public void Run(IEnumerator routine){StartCoroutine(routine);}}}
