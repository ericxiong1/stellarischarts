import React, { useState, useEffect, useMemo } from 'react';
import { api } from '../api';
import type { Country, Snapshot, BudgetLineItem, SpeciesBreakdown } from '../api';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
} from 'recharts';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import {
  Zap,
  Gem,
  Apple,
  TrendingUp,
  Hammer,
  Boxes,
  Sparkles,
  FlaskConical,
  Flame,
  Wind,
  Moon,
  Magnet,
  Droplets,
  Cpu,
  Crown,
  ChevronUp,
  ChevronDown,
} from 'lucide-react';

interface CountryDetailProps {
  country: Country | null;
}

export const CountryDetail: React.FC<CountryDetailProps> = ({ country }) => {
  const [snapshots, setSnapshots] = useState<Snapshot[]>([]);
  const [selectedSnapshot, setSelectedSnapshot] = useState<Snapshot | null>(null);
  const [budgetItems, setBudgetItems] = useState<BudgetLineItem[]>([]);
  const [previousBudgetItems, setPreviousBudgetItems] = useState<BudgetLineItem[]>([]);
  const [allCountries, setAllCountries] = useState<Country[]>([]);
  const [speciesBreakdown, setSpeciesBreakdown] = useState<SpeciesBreakdown[]>([]);
  const [previousSpeciesBreakdown, setPreviousSpeciesBreakdown] = useState<SpeciesBreakdown[]>([]);
  const [speciesHistory, setSpeciesHistory] = useState<Map<number, SpeciesBreakdown[]>>(new Map());
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!country) return;

    const fetchSnapshots = async () => {
      try {
        setLoading(true);
        const data = await api.getCountrySnapshots(country.countryId);
        setSnapshots(data);
        if (data.length > 0) {
          setSelectedSnapshot(data[0]);
        }
      } catch (err) {
        console.error('Failed to fetch snapshots:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchSnapshots();
  }, [country]);

  useEffect(() => {
    const fetchCountries = async () => {
      try {
        const data = await api.getCountries();
        setAllCountries(data);
      } catch (err) {
        console.error('Failed to fetch countries:', err);
      }
    };

    fetchCountries();
  }, []);

  useEffect(() => {
    if (!selectedSnapshot) return;

    const fetchBudget = async () => {
      try {
        const data = await api.getSnapshotBudget(selectedSnapshot.id);
        setBudgetItems(data);
        const ordered = [...snapshots].sort(
          (a, b) => parseGameDate(a.gameDate) - parseGameDate(b.gameDate)
        );
        const currentIndex = ordered.findIndex((s) => s.id === selectedSnapshot.id);
        const prev = currentIndex > 0 ? ordered[currentIndex - 1] : null;
        if (prev) {
          const prevData = await api.getSnapshotBudget(prev.id);
          setPreviousBudgetItems(prevData);
          const prevSpecies = await api.getSnapshotSpecies(prev.id);
          setPreviousSpeciesBreakdown(prevSpecies);
        } else {
          setPreviousBudgetItems([]);
          setPreviousSpeciesBreakdown([]);
        }
        const species = await api.getSnapshotSpecies(selectedSnapshot.id);
        setSpeciesBreakdown(species);
      } catch (err) {
        console.error('Failed to fetch budget:', err);
      }
    };

    fetchBudget();
  }, [selectedSnapshot, snapshots]);

  useEffect(() => {
    if (snapshots.length === 0) {
      setSpeciesHistory(new Map());
      return;
    }

    const fetchSpeciesHistory = async () => {
      try {
        const entries = await Promise.all(
          snapshots.map(async (snapshot) => {
            const data = await api.getSnapshotSpecies(snapshot.id);
            return [snapshot.id, data] as const;
          })
        );
        setSpeciesHistory(new Map(entries));
      } catch (err) {
        console.error('Failed to fetch species history:', err);
      }
    };

    fetchSpeciesHistory();
  }, [snapshots]);

  const incomeBudget = budgetItems.filter((b) => b.section === 'income');
  const expenseBudget = budgetItems.filter((b) => b.section === 'expenses');

  const groupedIncome = groupBudgetItems(incomeBudget);
  const groupedExpense = groupBudgetItems(expenseBudget);

  const incomeTotals = useMemo(() => {
    const totals: Record<string, number> = {};
    for (const item of incomeBudget) {
      totals[item.resourceType] = (totals[item.resourceType] ?? 0) + Number(item.amount);
    }
    return totals;
  }, [incomeBudget]);

  const expenseTotals = useMemo(() => {
    const totals: Record<string, number> = {};
    for (const item of expenseBudget) {
      totals[item.resourceType] = (totals[item.resourceType] ?? 0) + Number(item.amount);
    }
    return totals;
  }, [expenseBudget]);

  const previousIncomeTotals = useMemo(() => {
    const totals: Record<string, number> = {};
    for (const item of previousBudgetItems) {
      if (item.section !== 'income') continue;
      totals[item.resourceType] = (totals[item.resourceType] ?? 0) + Number(item.amount);
    }
    return totals;
  }, [previousBudgetItems]);

  const previousExpenseTotals = useMemo(() => {
    const totals: Record<string, number> = {};
    for (const item of previousBudgetItems) {
      if (item.section !== 'expenses') continue;
      totals[item.resourceType] = (totals[item.resourceType] ?? 0) + Number(item.amount);
    }
    return totals;
  }, [previousBudgetItems]);

  const netTotals = useMemo(() => {
    const totals: Record<string, number> = {};
    for (const item of budgetItems) {
      if (item.section !== 'income' && item.section !== 'expenses') continue;
      const sign = item.section === 'income' ? 1 : -1;
      totals[item.resourceType] = (totals[item.resourceType] ?? 0) + sign * Number(item.amount);
    }
    return totals;
  }, [budgetItems]);

  const previousNetTotals = useMemo(() => {
    const totals: Record<string, number> = {};
    for (const item of previousBudgetItems) {
      if (item.section !== 'income' && item.section !== 'expenses') continue;
      const sign = item.section === 'income' ? 1 : -1;
      totals[item.resourceType] = (totals[item.resourceType] ?? 0) + sign * Number(item.amount);
    }
    return totals;
  }, [previousBudgetItems]);

  const chartData = useMemo(() => {
    return [...snapshots]
      .sort((a, b) => parseGameDate(a.gameDate) - parseGameDate(b.gameDate))
      .map((s) => ({
        gameDateMs: parseGameDate(s.gameDate),
        gameDate: s.gameDate,
        military: Number(s.militaryPower),
        economy: Number(s.economyPower),
        tech: Number(s.techPower),
        pops: Number(s.numSapientPops),
        fleet: Number(s.fleetSize),
        empire: Number(s.empireSize),
      }));
  }, [snapshots]);

  const speciesChartData = useMemo(
    () => groupSpeciesData(speciesBreakdown, 2),
    [speciesBreakdown]
  );

  const previousSpeciesChartData = useMemo(
    () => groupSpeciesData(previousSpeciesBreakdown, 2),
    [previousSpeciesBreakdown]
  );

  const speciesDeltaByName = useMemo(() => {
    const prevMap = new Map(previousSpeciesChartData.map((item) => [item.speciesName, item.amount]));
    const current = new Map(speciesChartData.map((item) => [item.speciesName, item.amount]));
    const allNames = new Set([...prevMap.keys(), ...current.keys()]);
    const result = new Map<string, number>();
    allNames.forEach((name) => {
      const prev = prevMap.get(name) ?? 0;
      const curr = current.get(name) ?? 0;
      result.set(name, curr - prev);
    });
    return result;
  }, [previousSpeciesChartData, speciesChartData]);

  const speciesTrendData = useMemo(() => {
    if (snapshots.length === 0 || speciesHistory.size === 0) return [];
    const ordered = [...snapshots].sort((a, b) => parseGameDate(a.gameDate) - parseGameDate(b.gameDate));
    const trendSpecies = speciesChartData.map((item) => item.speciesName);
    const topSpecies = trendSpecies.filter((name) => name !== 'Other');

    return ordered.map((snapshot) => {
      const snapshotSpecies = speciesHistory.get(snapshot.id) ?? [];
      const map = new Map(snapshotSpecies.map((s) => [s.speciesName, s.amount]));
      const total = snapshotSpecies.reduce((sum, s) => sum + s.amount, 0);
      const row: Record<string, number | string> = { gameDate: snapshot.gameDate };
      for (const name of topSpecies) {
        row[name] = Number(map.get(name) ?? 0);
      }
      if (trendSpecies.includes('Other')) {
        const knownTotal = topSpecies.reduce((sum, name) => sum + Number(map.get(name) ?? 0), 0);
        row.Other = Math.max(0, total - knownTotal);
      }
      return row;
    });
  }, [snapshots, speciesHistory, speciesChartData]);

  const speciesColors = [
    '#3b82f6',
    '#22c55e',
    '#f59e0b',
    '#ef4444',
    '#a855f7',
    '#ec4899',
    '#14b8a6',
    '#eab308',
    '#0ea5e9',
    '#f97316',
    '#8b5cf6',
    '#84cc16',
  ];

  const rankByMetric = useMemo(() => {
    const rankMap = new Map<string, number>();
    if (!country || allCountries.length === 0) return rankMap;

    const metrics: Array<[string, (c: Country) => number, 'desc' | 'asc'?]> = [
      ['militaryPower', (c) => Number(c.militaryPower)],
      ['economyPower', (c) => Number(c.economyPower)],
      ['techPower', (c) => Number(c.techPower)],
      ['fleetSize', (c) => Number(c.fleetSize)],
      ['empireSize', (c) => Number(c.empireSize)],
      ['victoryRank', (c) => Number(c.victoryRank), 'asc'],
    ];

    for (const [key, getValue, order = 'desc'] of metrics) {
      const currentValue = getValue(country);
      const betterCount =
        order === 'asc'
          ? allCountries.filter((c) => getValue(c) < currentValue).length
          : allCountries.filter((c) => getValue(c) > currentValue).length;
      rankMap.set(key, betterCount + 1);
    }

    return rankMap;
  }, [allCountries, country]);

  const metaItems = useMemo(() => {
    if (!country) return [];
    return [
      { label: 'Civics', value: country.civics },
      { label: 'Traditions', value: country.traditionTrees },
      { label: 'Ascension Perks', value: country.ascensionPerks },
      { label: 'Federation', value: country.federationType },
      { label: 'Subject Status', value: country.subjectStatus },
      { label: 'Diplomatic Stance', value: country.diplomaticStance },
      { label: 'Diplo Weight', value: country.diplomaticWeight },
    ].filter((item) => item.value && item.value.trim().length > 0);
  }, [country]);

  if (!country) {
    return (
      <div className="flex flex-1 items-center justify-center bg-background p-8 text-muted-foreground">
        <p>Select a country to view details</p>
      </div>
    );
  }

  return (
    <div className="flex-1 overflow-y-auto bg-background p-8 text-foreground">
      <Card className="mb-6 border-border bg-gradient-to-b from-[#151515] to-[#0f0f0f] shadow-md">
        <CardHeader className="pb-4">
          <div className="flex flex-col gap-6 lg:flex-row lg:items-start lg:justify-between">
            <div>
              <CardTitle className="text-2xl">{country.name}</CardTitle>
              <p className="mt-1 text-sm italic text-muted-foreground">{country.adjective}</p>
              <div className="mt-3 flex flex-wrap gap-2">
                <Badge variant="secondary">{country.governmentType}</Badge>
                <Badge variant="outline">{country.authority}</Badge>
              </div>
              {country.ethos && (
                <div className="mt-2 flex flex-wrap gap-2">
                  {country.ethos.split(',').map((ethic) => (
                    <Badge key={ethic.trim()} variant="outline">
                      {ethic.trim()}
                    </Badge>
                  ))}
                </div>
              )}
              {metaItems.length > 0 && (
                <div className="mt-4 grid gap-3 text-xs text-muted-foreground sm:grid-cols-2">
                  {metaItems.map((item) => (
                    <div key={item.label} className="flex flex-col gap-1">
                      <span className="text-[10px] font-bold uppercase tracking-wide text-foreground">
                        {item.label}
                      </span>
                      <span
                        className={
                          item.label === 'Diplomatic Stance' && isGenocidalStance(item.value)
                            ? 'font-semibold text-red-500'
                            : 'text-foreground/90'
                        }
                      >
                        {item.value}
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </div>
            <div className="flex w-full flex-col gap-4 lg:w-auto lg:min-w-[360px]">
              <div className="text-right text-sm font-semibold text-foreground">{country.name}</div>
              <div className="grid grid-cols-2 gap-3">
                <div className="rounded-lg border border-border bg-black/30 p-3">
                  <div className="text-[11px] uppercase text-muted-foreground">Military Power</div>
                  <div className="flex items-baseline justify-between gap-2">
                    <div className="text-lg font-semibold text-red-400">{formatCompact(country.militaryPower)}</div>
                    <div className="text-xs text-muted-foreground">#{rankByMetric.get('militaryPower') ?? '-'}</div>
                  </div>
                </div>
                <div className="rounded-lg border border-border bg-black/30 p-3">
                  <div className="text-[11px] uppercase text-muted-foreground">Economic Power</div>
                  <div className="flex items-baseline justify-between gap-2">
                    <div className="text-lg font-semibold text-emerald-400">{formatCompact(country.economyPower)}</div>
                    <div className="text-xs text-muted-foreground">#{rankByMetric.get('economyPower') ?? '-'}</div>
                  </div>
                </div>
                <div className="rounded-lg border border-border bg-black/30 p-3">
                  <div className="text-[11px] uppercase text-muted-foreground">Tech Power</div>
                  <div className="flex items-baseline justify-between gap-2">
                    <div className="text-lg font-semibold text-blue-400">{formatCompact(country.techPower)}</div>
                    <div className="text-xs text-muted-foreground">#{rankByMetric.get('techPower') ?? '-'}</div>
                  </div>
                </div>
                <div className="rounded-lg border border-border bg-black/30 p-3">
                  <div className="text-[11px] uppercase text-muted-foreground">Fleet Size</div>
                  <div className="flex items-baseline justify-between gap-2">
                    <div className="text-lg font-semibold">{country.fleetSize}</div>
                    <div className="text-xs text-muted-foreground">#{rankByMetric.get('fleetSize') ?? '-'}</div>
                  </div>
                </div>
                <div className="rounded-lg border border-border bg-black/30 p-3">
                  <div className="text-[11px] uppercase text-muted-foreground">Empire Size</div>
                  <div className="flex items-baseline justify-between gap-2">
                    <div className="text-lg font-semibold">{country.empireSize}</div>
                    <div className="text-xs text-muted-foreground">#{rankByMetric.get('empireSize') ?? '-'}</div>
                  </div>
                </div>
                <div className="rounded-lg border border-border bg-black/30 p-3">
                  <div className="text-[11px] uppercase text-muted-foreground">Galactic Rank</div>
                  <div className="text-lg font-semibold">#{country.victoryRank}</div>
                </div>
              </div>
            </div>
          </div>
        </CardHeader>
      </Card>

      {loading ? (
        <p className="text-muted-foreground">Loading data...</p>
      ) : snapshots.length > 0 ? (
        <div>
          <h2 className="mb-3 text-xl font-semibold">Monthly Net Resources</h2>
          {(() => {
            const resourceCards = [
              { key: 'energy', label: 'Energy Credits', Icon: Zap, iconClass: 'text-yellow-300' },
              { key: 'minerals', label: 'Minerals', Icon: Gem, iconClass: 'text-red-400' },
              { key: 'food', label: 'Food', Icon: Apple, iconClass: 'text-emerald-300' },
              { key: 'trade_value', label: 'Trade', Icon: TrendingUp, iconClass: 'text-white' },
              { key: 'alloys', label: 'Alloys', Icon: Hammer, iconClass: 'text-orange-300' },
              { key: 'consumer_goods', label: 'Consumer Goods', Icon: Boxes, iconClass: 'text-cyan-300' },
              { key: 'unity', label: 'Unity', Icon: Sparkles, iconClass: 'text-fuchsia-300' },
              { key: 'research', label: 'Research', Icon: FlaskConical, iconClass: 'text-sky-300' },
              { key: 'volatile_motes', label: 'Volatile Motes', Icon: Flame, iconClass: 'text-orange-300' },
              { key: 'rare_crystals', label: 'Rare Crystals', Icon: Gem, iconClass: 'text-pink-300' },
              { key: 'exotic_gases', label: 'Exotic Gases', Icon: Wind, iconClass: 'text-cyan-300' },
              { key: 'influence', label: 'Influence', Icon: Crown, iconClass: 'text-yellow-200' },
              { key: 'dark_matter', label: 'Dark Matter', Icon: Moon, iconClass: 'text-indigo-300' },
              { key: 'living_metal', label: 'Living Metal', Icon: Magnet, iconClass: 'text-emerald-300' },
              { key: 'zro', label: 'Zro', Icon: Droplets, iconClass: 'text-violet-300' },
              { key: 'minor_artifacts', label: 'Minor Artifacts', Icon: Sparkles, iconClass: 'text-amber-300' },
              { key: 'astral_threads', label: 'Astral Threads', Icon: Wind, iconClass: 'text-teal-300' },
              { key: 'nanites', label: 'Nanites', Icon: Cpu, iconClass: 'text-slate-200' },
            ].filter((resource) => shouldShowResource(resource.key, incomeTotals, expenseTotals));

            if (resourceCards.length === 0) return null;

            return (
              <div className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
                {resourceCards.map((resource) => {
                  const value = resolveResource(netTotals, resource.key);
                  const incomeValue = resolveResource(incomeTotals, resource.key);
                  const expenseValue = resolveResource(expenseTotals, resource.key);
                  const prevIncomeValue = resolveResource(previousIncomeTotals, resource.key);
                  const prevExpenseValue = resolveResource(previousExpenseTotals, resource.key);
                  const prevValue = resolveResource(previousNetTotals, resource.key);
                  const deltaPct =
                    prevValue === 0 ? null : ((value - prevValue) / Math.abs(prevValue)) * 100;
                  const deltaValue = value - prevValue;
                  const valueClass = value >= 0 ? 'text-emerald-400' : 'text-red-400';
                  const deltaClass = deltaValue >= 0 ? 'text-emerald-300' : 'text-red-400';
                  const DeltaIcon = deltaValue >= 0 ? ChevronUp : ChevronDown;
                  const expenseNetValue = Math.abs(expenseValue);
                  const prevExpenseNetValue = Math.abs(prevExpenseValue);
                  const incomeDeltaValue = incomeValue - prevIncomeValue;
                  const expenseDeltaValue = expenseNetValue - prevExpenseNetValue;
                  const incomeDeltaPct =
                    prevIncomeValue === 0
                      ? null
                      : (incomeDeltaValue / Math.abs(prevIncomeValue)) * 100;
                  const expenseDeltaPct =
                    prevExpenseNetValue === 0
                      ? null
                      : (expenseDeltaValue / Math.abs(prevExpenseNetValue)) * 100;
                  const incomeDeltaClass = incomeDeltaValue >= 0 ? 'text-emerald-300' : 'text-red-400';
                  const expenseDeltaClass = expenseDeltaValue >= 0 ? 'text-red-400' : 'text-emerald-300';
                  const researchIncome =
                    resource.key === 'research' ? getResearchBreakdown(incomeTotals) : null;
                  const researchPrevIncome =
                    resource.key === 'research' ? getResearchBreakdown(previousIncomeTotals) : null;
                  return (
                    <Card
                      key={resource.key}
                      className="border-border bg-gradient-to-b from-[#151515] to-[#0f0f0f] shadow-md"
                    >
                      <CardContent className="space-y-4 p-4">
                        <div className="flex items-center justify-between">
                          <div className="rounded-lg bg-black/40 p-2">
                            <resource.Icon className={`h-4 w-4 ${resource.iconClass}`} />
                          </div>
                          {deltaPct === null ? (
                            <span className="text-xs text-muted-foreground"></span>
                          ) : (
                            <div className={`flex items-center gap-1 text-xs font-semibold ${deltaClass}`}>
                              <span>
                                {formatDelta(deltaPct)}
                                {` (${formatSigned(deltaValue)})`}
                              </span>
                              <DeltaIcon className="h-3.5 w-3.5" />
                            </div>
                          )}
                        </div>
                        <div>
                          <div className={`text-2xl font-semibold ${valueClass}`}>{formatSigned(value)}</div>
                          <div className="text-xs text-muted-foreground">{resource.label}</div>
                        </div>
                        <div className="space-y-1 text-xs text-muted-foreground">
                          {resource.key === 'research' && researchIncome && researchPrevIncome ? (
                            <>
                              {[
                                { key: 'physics', label: 'Physics' },
                                { key: 'society', label: 'Society' },
                                { key: 'engineering', label: 'Engineering' },
                              ].map((line) => {
                                const current =
                                  researchIncome[line.key as 'physics' | 'society' | 'engineering'];
                                const previous =
                                  researchPrevIncome[line.key as 'physics' | 'society' | 'engineering'];
                                const deltaValue = current - previous;
                                const deltaPct =
                                  previous === 0 ? null : (deltaValue / Math.abs(previous)) * 100;
                                const deltaClass = deltaValue >= 0 ? 'text-emerald-300' : 'text-red-400';
                                return (
                                  <div key={line.key} className="flex items-center justify-between">
                                    <span>{line.label}</span>
                                    <span className="font-semibold text-emerald-300">
                                      {formatCompact(current)}
                                      {deltaPct === null ? '' : (
                                        <span className={`ml-1 ${deltaClass}`}>
                                          ({formatSigned(deltaValue)}, {formatDelta(deltaPct)})
                                        </span>
                                      )}
                                    </span>
                                  </div>
                                );
                              })}
                            </>
                          ) : (
                            <div className="flex items-center justify-between">
                              <span>Gross Income</span>
                              <span className="font-semibold text-emerald-300">
                                {formatCompact(incomeValue)}
                                {incomeDeltaPct === null ? '' : (
                                  <span className={`ml-1 ${incomeDeltaClass}`}>
                                    ({formatSigned(incomeDeltaValue)}, {formatDelta(incomeDeltaPct)})
                                  </span>
                                )}
                              </span>
                            </div>
                          )}
                          {resource.key === 'research' ? null : (
                            <div className="flex items-center justify-between">
                              <span>Net Expenses</span>
                              <span className="font-semibold text-red-400">
                                {formatCompact(expenseNetValue)}
                                {expenseDeltaPct === null ? '' : (
                                  <span className={`ml-1 ${expenseDeltaClass}`}>
                                    ({formatSigned(expenseDeltaValue)}, {formatDelta(expenseDeltaPct)})
                                  </span>
                                )}
                              </span>
                            </div>
                          )}
                        </div>
                      </CardContent>
                    </Card>
                  );
                })}
              </div>
            );
          })()}
          <h2 className="mb-3 text-xl font-semibold">Population Demographics</h2>
          <div className="mb-6 grid gap-4 lg:grid-cols-2">
            <Card className="border-border">
              <CardContent className="p-4">
                {speciesChartData.length === 0 ? (
                  <p className="text-sm text-muted-foreground">No species data available</p>
                ) : (
                  <div className="h-72">
                    <ResponsiveContainer width="100%" height="100%">
                      <PieChart>
                        <Pie
                          data={speciesChartData}
                          dataKey="amount"
                          nameKey="speciesName"
                          cx="50%"
                          cy="50%"
                          outerRadius={100}
                          label={false}
                          labelLine={false}
                        >
                          {speciesChartData.map((entry, index) => (
                            <Cell
                              key={`${entry.speciesName}-${index}`}
                              fill={speciesColors[index % speciesColors.length]}
                            />
                          ))}
                        </Pie>
                        <Tooltip
                          content={
                            <SpeciesTooltip
                              total={sumSpeciesAmount(speciesChartData)}
                              deltaByName={speciesDeltaByName}
                            />
                          }
                        />
                        <Legend content={<SpeciesLegend data={speciesChartData} colors={speciesColors} />} />
                      </PieChart>
                    </ResponsiveContainer>
                  </div>
                )}
              </CardContent>
            </Card>
            <Card className="border-border">
              <CardContent className="p-4">
                {speciesTrendData.length === 0 ? (
                  <p className="text-sm text-muted-foreground">No species history available</p>
                ) : (
                  <div className="h-72">
                    <ResponsiveContainer width="100%" height="100%">
                      <LineChart data={speciesTrendData}>
                        <CartesianGrid strokeDasharray="3 3" stroke="oklch(var(--border))" />
                        <XAxis
                          dataKey="gameDate"
                          type="category"
                          tick={{ fill: 'oklch(var(--muted-foreground))', fontSize: 12 }}
                        />
                        <YAxis
                          tick={{ fill: 'oklch(var(--muted-foreground))', fontSize: 12 }}
                          tickFormatter={(value) => formatCompact(Number(value))}
                          allowDecimals={false}
                        />
                        <Tooltip
                          contentStyle={{
                            background: 'oklch(var(--card))',
                            border: '1px solid oklch(var(--border))',
                          }}
                          labelStyle={{ color: 'oklch(var(--muted-foreground))' }}
                          labelFormatter={(_, payload) => payload?.[0]?.payload?.gameDate ?? ''}
                        />
                        <Legend />
                        {speciesChartData.map((species, index) => (
                          <Line
                            key={species.speciesName}
                            type="monotone"
                            dataKey={species.speciesName}
                            stroke={speciesColors[index % speciesColors.length]}
                            dot={false}
                          />
                        ))}
                      </LineChart>
                    </ResponsiveContainer>
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
          {chartData.length > 0 && (
            <div>
              <h2 className="mb-3 mt-2 text-xl font-semibold">Power Over Time</h2>
              <Card className="mb-6 border-border">
                <CardContent className="p-4">
                <ResponsiveContainer width="100%" height={300}>
                  <LineChart data={chartData}>
                    <CartesianGrid strokeDasharray="3 3" stroke="oklch(var(--border))" />
                    <XAxis
                      dataKey="gameDate"
                      type="category"
                      tick={{ fill: 'oklch(var(--muted-foreground))', fontSize: 12 }}
                    />
                    <YAxis tick={{ fill: 'oklch(var(--muted-foreground))', fontSize: 12 }} />
                    <Tooltip
                      contentStyle={{ background: 'oklch(var(--card))', border: '1px solid oklch(var(--border))' }}
                      labelStyle={{ color: 'oklch(var(--muted-foreground))' }}
                      labelFormatter={(_, payload) => payload?.[0]?.payload?.gameDate ?? ''}
                    />
                    <Legend />
                    <Line type="monotone" dataKey="military" stroke="#ff6b6b" dot={false} />
                    <Line type="monotone" dataKey="economy" stroke="#51cf66" dot={false} />
                    <Line type="monotone" dataKey="tech" stroke="#748ffc" dot={false} />
                  </LineChart>
                </ResponsiveContainer>
                </CardContent>
              </Card>

              <h2 className="mb-3 text-xl font-semibold">Population & Capacity</h2>
              <Card className="mb-8 border-border">
                <CardContent className="p-4">
                <ResponsiveContainer width="100%" height={280}>
                  <LineChart data={chartData}>
                    <CartesianGrid strokeDasharray="3 3" stroke="oklch(var(--border))" />
                    <XAxis
                      dataKey="gameDate"
                      type="category"
                      tick={{ fill: 'oklch(var(--muted-foreground))', fontSize: 12 }}
                    />
                    <YAxis tick={{ fill: 'oklch(var(--muted-foreground))', fontSize: 12 }} />
                    <Tooltip
                      contentStyle={{ background: 'oklch(var(--card))', border: '1px solid oklch(var(--border))' }}
                      labelStyle={{ color: 'oklch(var(--muted-foreground))' }}
                      labelFormatter={(_, payload) => payload?.[0]?.payload?.gameDate ?? ''}
                    />
                    <Legend />
                    <Line type="monotone" dataKey="pops" stroke="#ffd43b" dot={false} />
                    <Line type="monotone" dataKey="fleet" stroke="#ffa94d" dot={false} />
                    <Line type="monotone" dataKey="empire" stroke="#63e6be" dot={false} />
                  </LineChart>
                </ResponsiveContainer>
                </CardContent>
              </Card>
            </div>
          )}
          <Separator className="my-6" />
          <h2 className="mb-4 text-xl font-semibold">Budget Analysis</h2>
          <div className="grid gap-8 lg:grid-cols-2">
            <div>
              <h3 className="mb-3 text-base font-semibold">Income Sources</h3>
              {Object.entries(groupedIncome).map(([category, items]) => (
                <div key={category} className="mb-5">
                  <div className="mb-2 text-xs font-semibold uppercase text-muted-foreground">
                    {formatCategoryName(category)}
                  </div>
                  {items.map((item) => (
                    <div
                      key={item.id}
                      className="mb-1 flex items-center justify-between border-l-2 border-emerald-400 pl-3 text-sm text-emerald-400"
                    >
                      <span className="capitalize">{item.resourceType}</span>
                      <span className="font-semibold">+{item.amount.toFixed(2)}</span>
                    </div>
                  ))}
                </div>
              ))}
            </div>
            <div>
              <h3 className="mb-3 text-base font-semibold">Expenses</h3>
              {Object.entries(groupedExpense).map(([category, items]) => (
                <div key={category} className="mb-5">
                  <div className="mb-2 text-xs font-semibold uppercase text-muted-foreground">
                    {formatCategoryName(category)}
                  </div>
                  {items.map((item) => (
                    <div
                      key={item.id}
                      className="mb-1 flex items-center justify-between border-l-2 border-red-400 pl-3 text-sm text-red-400"
                    >
                      <span className="capitalize">{item.resourceType}</span>
                      <span className="font-semibold">-{item.amount.toFixed(2)}</span>
                    </div>
                  ))}
                </div>
              ))}
            </div>
          </div>
        </div>
      ) : (
        <p className="text-muted-foreground">No snapshot data available</p>
      )}
    </div>
  );
};

function groupBudgetItems(items: BudgetLineItem[]): Record<string, BudgetLineItem[]> {
  return items.reduce(
    (acc, item) => {
      if (!acc[item.category]) {
        acc[item.category] = [];
      }
      acc[item.category].push(item);
      return acc;
    },
    {} as Record<string, BudgetLineItem[]>
  );
}

function formatCategoryName(name: string): string {
  return name
    .replace(/_/g, ' ')
    .split(' ')
    .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
    .join(' ');
}

function formatCompact(value: number): string {
  const abs = Math.abs(value);
  if (abs >= 1_000_000) return `${(value / 1_000_000).toFixed(2)}M`;
  if (abs >= 1_000) return `${(value / 1_000).toFixed(2)}K`;
  return value.toFixed(2);
}

function formatDelta(value: number): string {
  const sign = value >= 0 ? '+' : '';
  return `${sign}${value.toFixed(0)}%`;
}

function formatSigned(value: number): string {
  const sign = value >= 0 ? '+' : '';
  return `${sign}${formatCompact(value)}`;
}

function resolveResource(source: Record<string, number>, key: string): number {
  if (!source) return 0;
  if (key === 'trade_value') {
    return source.trade_value ?? source.trade ?? 0;
  }
  if (key === 'research') {
    let total = 0;
    for (const [resourceKey, value] of Object.entries(source)) {
      if (resourceKey === 'research' || resourceKey.endsWith('_research')) {
        total += Number(value);
      }
    }
    return total;
  }
  return source[key] ?? 0;
}

function shouldShowResource(
  key: string,
  incomeTotals: Record<string, number>,
  expenseTotals: Record<string, number>
): boolean {
  if (key === 'research') {
    const breakdown = getResearchBreakdown(incomeTotals);
    const totalIncome = breakdown.physics + breakdown.society + breakdown.engineering;
    const totalExpense = resolveResource(expenseTotals, key);
    return totalIncome !== 0 || totalExpense !== 0;
  }

  const incomeValue = resolveResource(incomeTotals, key);
  const expenseValue = resolveResource(expenseTotals, key);
  return incomeValue !== 0 || expenseValue !== 0;
}

function parseGameDate(value: string): number {
  const match = /^(\d{4})\.(\d{2})\.(\d{2})$/.exec(value);
  if (!match) {
    return new Date(value).getTime();
  }
  const year = Number(match[1]);
  const month = Number(match[2]) - 1;
  const day = Number(match[3]);
  return Date.UTC(year, month, day);
}

function groupSpeciesData(data: SpeciesBreakdown[], minPercent: number): SpeciesBreakdown[] {
  if (data.length === 0) return [];
  const sorted = [...data].sort((a, b) => b.amount - a.amount);
  const total = sorted.reduce((sum, item) => sum + item.amount, 0);
  if (total <= 0) return sorted;

  const kept: SpeciesBreakdown[] = [];
  let otherTotal = 0;
  for (const item of sorted) {
    const pct = (item.amount / total) * 100;
    if (pct < minPercent) {
      otherTotal += item.amount;
    } else {
      kept.push(item);
    }
  }

  if (otherTotal > 0) {
    kept.push({ speciesName: 'Other', amount: otherTotal });
  }

  const withoutOther = kept.filter((item) => item.speciesName !== 'Other');
  const otherItem = kept.find((item) => item.speciesName === 'Other');
  return otherItem ? [...withoutOther, otherItem] : withoutOther;
}

function sumSpeciesAmount(data: SpeciesBreakdown[]): number {
  return data.reduce((sum, item) => sum + item.amount, 0);
}

function formatWhole(value: number): string {
  return Math.round(value).toLocaleString();
}

function formatSignedWhole(value: number): string {
  const sign = value >= 0 ? '+' : '';
  return `${sign}${formatWhole(value)}`;
}

function isGenocidalStance(value: string): boolean {
  const stance = value.toLowerCase();
  return (
    stance.includes('purification') ||
    stance.includes('hunger') ||
    stance.includes('extermination') ||
    stance.includes('devastators')
  );
}

function SpeciesTooltip({
  active,
  payload,
  total,
  deltaByName,
}: {
  active?: boolean;
  payload?: any[];
  total: number;
  deltaByName?: Map<string, number>;
}) {
  if (!active || !payload?.length) return null;
  const entry = payload[0];
  const value = Number(entry.value ?? 0);
  const percent = total > 0 ? (value / total) * 100 : 0;
  const delta = deltaByName?.get(entry.name ?? '') ?? 0;
  const deltaText = delta === 0 ? '+0' : formatSignedWhole(delta);
  const deltaColor = delta >= 0 ? 'rgb(52 211 153)' : 'rgb(248 113 113)';
  return (
    <div
      style={{
        background: 'oklch(var(--background))',
        border: '1px solid oklch(var(--border))',
        borderRadius: '8px',
        padding: '8px 10px',
        color: 'oklch(var(--foreground))',
      }}
    >
      <div style={{ fontWeight: 600, marginBottom: 4 }}>{entry.name ?? 'Species'}</div>
      <div style={{ fontSize: 12, opacity: 0.9 }}>
        {formatWhole(value)} ({percent.toFixed(1)}%)
      </div>
      <div style={{ fontSize: 12, marginTop: 4, color: deltaColor }}>
        Δ {deltaText}
      </div>
    </div>
  );
}

function SpeciesLegend({ data, colors }: { data: SpeciesBreakdown[]; colors: string[] }) {
  return (
    <div style={{ display: 'flex', flexWrap: 'wrap', gap: '10px 16px', fontSize: 14, marginTop: 10 }}>
      {data.map((item, index) => (
        <div key={`${item.speciesName}-${index}`} style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <span
            style={{
              width: 12,
              height: 12,
              borderRadius: 999,
              background: colors[index % colors.length],
              display: 'inline-block',
            }}
          />
          <span>{item.speciesName}</span>
        </div>
      ))}
    </div>
  );
}
function getResearchBreakdown(source: Record<string, number>): {
  physics: number;
  society: number;
  engineering: number;
} {
  return {
    physics: source.physics_research ?? source.physics ?? 0,
    society: source.society_research ?? source.society ?? 0,
    engineering: source.engineering_research ?? source.engineering ?? 0,
  };
}
