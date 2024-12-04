using UnityEngine;

// Estado: Listo para Cosechar 
public class FarmPlotReadyState : FarmPlotBaseState
{
    public FarmPlotReadyState(FarmPlotStateManager manager, FarmPlotData plotData)
        : base(manager, plotData) { }

    public override void Enter()
    {
        Debug.Log("Parcela en estado: Lista para cosechar");
        // Cambiar a visual de cultivo maduro
        if (plotData.plotRenderer != null)
        {
            plotData.plotRenderer.material.color = Color.green;
        }
    }

    public override void Update() { }

    public override void Exit()
    {
        Debug.Log("Cosecha completada");
    }

    public override string GetInteractionPrompt(FarmingTool currentTool)
    {
        return "Presiona E para cosechar";
    }

    public override bool CanInteract(FarmingTool currentTool)
    {
        return true; // Se puede cosechar con cualquier herramienta o manos vac�as
    }

    public override void OnInteract(FarmingTool currentTool)
    {
        // Aqu� a�adiremos la l�gica para:
        // 1. A�adir el cultivo al inventario del jugador
        // 2. Reproducir efectos de part�culas
        // 3. Reproducir sonido de cosecha

        Debug.Log("�Cultivo cosechado!");

        // Volver al estado inicial
        manager.ChangeState(manager.UnplowedState);
    }
}
