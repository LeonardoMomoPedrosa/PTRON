# Custom Cost Preview Feature - Usage Guide

## Overview
This feature allows you to estimate the cost of producing equipment when some insumos are not available in stock, which is useful for pre-sales calculations.

## How It Works

### Normal Production Flow (All Items Available)
1. Select an equipment model
2. Enter quantity
3. System shows all insumos with green checkmarks
4. "Produzir" button is enabled
5. Cost is calculated based on stored unit costs

### Preview Mode (Missing Items)
1. Select an equipment model
2. Enter quantity
3. System shows red error for missing insumos
4. A hint message appears: "💡 Você pode inserir custos customizados abaixo para estimar o custo de pré-venda"
5. For each missing insumo, an input field appears in the "Custo unit." column
6. Enter estimated costs based on current market prices, exchange rates, etc.
7. The system automatically recalculates:
   - Subtotal for that insumo
   - Total cost estimate
   - Cost per unit (when quantity > 1)
8. An info alert appears showing: "Modo de Prévia — Estimativa de Custo para Pré-Venda"
9. The "Produzir" button is DISABLED (this is preview only)

## UI Indicators

### When Custom Costs Are Active
- 🔵 Blue info alert at the top indicating preview mode
- 💵 Dollar icon in the input field
- 🔍 Preview icon next to subtotals
- 🧮 Calculator icon in status column
- Original cost shown below custom input for reference
- Footer shows "Prévia de custo" instead of "Custo estimado"

### Clearing Custom Costs
- Click the X button in any custom cost field to clear it
- When all custom costs are cleared, the system returns to normal mode

## Example Use Case

**Scenario**: Customer wants to pre-order 5 units of "Equipment X" but you're missing some resistors

**Steps**:
1. Go to /producao
2. Select "Equipment X"
3. Set quantity to 5
4. System shows resistors are missing (need 20, have 5)
5. Check current market price for resistors: R$ 2.50 each (considering exchange rate)
6. Enter "2.50" in the custom cost field for resistors
7. System calculates:
   - Resistors subtotal: 20 × R$ 2.50 = R$ 50.00
   - Plus other available insumos costs
   - Total preview cost: R$ XXX.XX
   - Cost per unit: R$ XX.XX
8. Use this information to quote the customer

## Important Notes

- ⚠️ Custom costs are NOT saved - they're for real-time calculation only
- ⚠️ Production is disabled when using custom costs
- ⚠️ This is a preview/estimation tool only
- ✅ Original unit costs are always displayed for reference
- ✅ You can mix available items (using stock costs) with missing items (using custom costs)
- ✅ All calculations update in real-time as you type

## Technical Details

### What Changed
- `BomLinhaPreview` now has `CustoCustomizado` (nullable)
- Calculations use `CustoEfetivo` (custom if set, otherwise unit cost)
- `ProducaoPreview.PodeProduzir` returns false when custom costs are active
- UI shows input fields only for missing items
- Real-time updates via `@bind-Value:after="StateHasChanged"`

### Data Flow
```
User enters custom cost 
  → CustoCustomizado updated
  → CustoEfetivo returns custom value
  → Subtotal recalculated
  → CustoEstimado recalculated
  → TemCustosCustomizados becomes true
  → PodeProduzir becomes false
  → UI updates to preview mode
```
