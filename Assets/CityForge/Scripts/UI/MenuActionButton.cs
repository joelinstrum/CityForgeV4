using UnityEngine;
using UnityEngine.UI;

namespace CityForge.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class MenuActionButton : MonoBehaviour
    {
        [SerializeField] private MainMenuController controller;
        [SerializeField] private MenuAction action;

        public void Configure(MainMenuController owner, MenuAction selectedAction)
        {
            controller = owner;
            action = selectedAction;
        }

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() => controller.Handle(action));
        }

    }
}
