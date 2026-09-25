import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { colors } from '../theme';
import type { RootStackParamList } from './types';
import { HomeScreen } from '../screens/HomeScreen';
import { SettingsScreen } from '../screens/SettingsScreen';
import { TiposScreen } from '../screens/TiposScreen';
import { TipoFormScreen } from '../screens/TipoFormScreen';
import { InsumosScreen } from '../screens/InsumosScreen';
import { InsumoFormScreen } from '../screens/InsumoFormScreen';
import { EquipamentosScreen } from '../screens/EquipamentosScreen';
import { EquipamentoFormScreen } from '../screens/EquipamentoFormScreen';
import { EntradasScreen } from '../screens/EntradasScreen';
import { EntradaNovaScreen } from '../screens/EntradaNovaScreen';
import { EntradaDetalheScreen } from '../screens/EntradaDetalheScreen';
import { ProducaoScreen } from '../screens/ProducaoScreen';
import { ProdutosScreen } from '../screens/ProdutosScreen';
import { ProdutoDetalheScreen } from '../screens/ProdutoDetalheScreen';

const Stack = createNativeStackNavigator<RootStackParamList>();

const screenOptions = {
  headerStyle: { backgroundColor: colors.surface },
  headerTintColor: colors.primaryDark,
  headerTitleStyle: { fontWeight: '700' as const },
  contentStyle: { backgroundColor: colors.bg },
};

export function RootNavigator() {
  return (
    <NavigationContainer>
      <Stack.Navigator initialRouteName="Home" screenOptions={screenOptions}>
        <Stack.Screen name="Home" component={HomeScreen} options={{ title: 'PTRON' }} />
        <Stack.Screen name="Settings" component={SettingsScreen} options={{ title: 'Configurações' }} />
        <Stack.Screen name="Tipos" component={TiposScreen} options={{ title: 'Tipos' }} />
        <Stack.Screen
          name="TipoForm"
          component={TipoFormScreen}
          options={({ route }) => ({ title: route.params?.id ? 'Editar tipo' : 'Novo tipo' })}
        />
        <Stack.Screen name="Insumos" component={InsumosScreen} options={{ title: 'Insumos' }} />
        <Stack.Screen
          name="InsumoForm"
          component={InsumoFormScreen}
          options={({ route }) => ({ title: route.params?.id ? 'Editar insumo' : 'Novo insumo' })}
        />
        <Stack.Screen
          name="Equipamentos"
          component={EquipamentosScreen}
          options={{ title: 'Equipamentos' }}
        />
        <Stack.Screen
          name="EquipamentoForm"
          component={EquipamentoFormScreen}
          options={({ route }) => ({
            title: route.params?.id ? 'Editar equipamento' : 'Novo equipamento',
          })}
        />
        <Stack.Screen name="Entradas" component={EntradasScreen} options={{ title: 'Entradas' }} />
        <Stack.Screen
          name="EntradaNova"
          component={EntradaNovaScreen}
          options={{ title: 'Nova entrada' }}
        />
        <Stack.Screen
          name="EntradaDetalhe"
          component={EntradaDetalheScreen}
          options={{ title: 'Detalhe da entrada' }}
        />
        <Stack.Screen name="Producao" component={ProducaoScreen} options={{ title: 'Produção' }} />
        <Stack.Screen name="Produtos" component={ProdutosScreen} options={{ title: 'Produtos' }} />
        <Stack.Screen
          name="ProdutoDetalhe"
          component={ProdutoDetalheScreen}
          options={{ title: 'Detalhe do produto' }}
        />
      </Stack.Navigator>
    </NavigationContainer>
  );
}
