Construa um aplicativo para controle de produção de equipamentos eletrônicos.
Será um aplicativo que terá cadastro de insumos (componentes), cadastro de equipamentos (modelos a serem produzidos), entrada de estoque e cadastro de produto final que irá
consumir os insumos e definir o preço de custo total da peça.
1 - Cadastro de tipo (tipo de componente, resistor, diodo, valvula etc)
 - Id
 - Nome (ex resistor, valvula)

2 - Cadastro de insumo (componente) - CRUD operations
 - Id
 - Id Tipo (diodo, resistor, CI, transistor, válvula, etc) - Tipo será escolhido da lista.
 - Nome
 - valor (ex 8ohm, 5uF) opcional
 - Potência (2W, 5W) opcional
 - Voltagem (35V, 400V) opcional
 - Saldo (esse campo não é cadastrado, ele é sempre 0 no cadastro do item)
 - Custo unitário (quando eu paguei no insumo) - Será zero durante o cadastro.
 - Foto - uma pequena foto do insumo (poderá ou não ter foto).

3 - Cadastro de equipamentos (Modelo) - CRUD operations
 - Id
 - Nome
 - Foto
 3a - Insumos do equipamento (mais de um por id de equipamento) - CRUD operations
 - Id equipamento
 - Id Insumo
 - Qtd (quantidade de insumos)

4 - Entrada de estoque (de insumos)
Realizar a entrada em estoque de insumos. Será do tipo "cart", eu insiro o insumo (selecionando uma lista, use autocomplete para facilitar).
Entrar: Quantidade, Preço unitário. Posso inserir vários componentes ao mesmo tempo.
Finalizando a entrada de estoque os insumos agora tem o estoque especificado. O custo final será a média com os que já estão em estoque. Por exemplo, tenho 2 capacitores X custo 10,00 cada. E insiro mais 3 capacitores X a um custo de 15,00. Portanto terei (2x10 + 3x15)/5 = 13. Assim o insumo terá um único custo unitário.

5 - Produção
Na parte de produção, irei selecionar o equipamento (modelo) e uma lista de estoque baseado nos insumos do modelo será mostrada na tela e o custo final será estimado.
Caso algum insumo não esteja disponível, não será possível fechar a produção e os faltantes serão mostrados na tela.
Caso todos os insumos estejam disponíveis para o equipamento, será possível produzir; Um produto será criado (veja item 6) e os insumos subtraídos do estoque.
Durante a produção, os seguintes campos serão preenchidos.
 - Descrição adicional do produto

Após selecionar o modelo, e digitar a descrição adicional do produto, revisar os insumos utilizados com respectivos custos e custo final, poderemos finalizar a produção, e uma
nova entrada de produto será criada.

6 - Cadastro de produtos - Só será possível ser feito automaticamente durante o fluxo acima (5 - Produção).
 - Id do produto
 - Id do equipamento (modelo)
 - Descrição adicional do produto (item 5)
6a - insumos do produto
 - Id produto
 - Id equipamento
 - Id Insumo
 - Preço unitário (vem do insumo)
 - Qtd (quantidade de insumos)

Devemos ter uma página para lista os produtos já produzidos e o custo unitário de cada um.
Será possível deletar um produto, nesse caso os insumos voltam ao estoque.
Caso um insumo seja adicionado ou removido a um equipamento (modelo), produtos já feitos não serão alterados, apenas se forem deletados e recriados.