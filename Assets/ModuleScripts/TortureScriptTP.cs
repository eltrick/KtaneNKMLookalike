using System;
using KeepCoding;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

public class TortureScriptTP : TPScript<TortureScript>
{
    private string TwitchHelpMessage = "<!{0} cycle [delay]> to press each tile in reading order once, with given delay; the delay can be in decimal form, e.g. 2.47. <!{0} <16 digits>> to construct the array for which to press each position in reading order that number of times. <!{0} [coordinate]> to press that specific coordinate, with the column being specified by letters, with the column after Z being AA, and the row being specified by a number. The first command type is not chainable, nor can you use both types of commands in one, however the second command type can have multiple coordinates separated by spaces. <!{0} <16 numbers separated by spaces>> to also construct a more precise array for which to press the buttons, similar to the first command. <!{0} (resize|setsize) [height] [width]> to resize the grid, and reset all values. However, width * height must be at least 9. This command is disallowed in a mission.";

    public override IEnumerator ForceSolve()
    {
        Module.IsLogging = true;
        Module.IsAutosolve = true;

        Module.PressLogKey();

        yield return null;
        for (int i = 0; i < Module._grid.Length; i++)
            while (Module.TwitchPlaysAutosolver[i] != 0)
            {
                Module._grid[i].Button.OnInteract();
                yield return null;
            }
        while (Module.IsModuleSolved)
            yield return true;
    }

    public override IEnumerator Process(string command)
    {
        if (Module.IsModuleSolved)
            yield break;

        command = command.ToUpperInvariant();

        yield return null;
        if (Regex.IsMatch(command, "CYCLE [0-9]+(\\.[0-9]+)?"))
        {
            float delay = float.Parse(Regex.Match(command, "[0-9]+(\\.[0-9]+)?").Value);
            for(int i = 0; i < Module._grid.Length; i++)
            {
                Module._grid[i].Button.OnInteract();
                yield return new WaitForSeconds(delay);
                yield return "trycancel";
            }
        }
        else if (Regex.IsMatch(command, "[0-9]{" + Module.GridSize.ToString() + "}") && Module.Modulus <= 10)
            for (int i = 0; i < Module._grid.Length; i++)
                for (int j = 0; j < int.Parse(command[i].ToString()) % Module.Modulus; j++)
                {
                    Module._grid[i].Button.OnInteract();
                    yield return new WaitForSeconds(.1f / Mathf.Pow(Module.GridSize / 16f, 1.5f));
                    yield return "trycancel";
                }
        else if (Regex.IsMatch(command, "([0-9]+\\s){" + (Module.GridSize - 1).ToString() + "}[0-9]+"))
        {
            string[] numbers = command.Split(" ");
            float time = 0;
            int p = 0;

            for(int i = 0; i < Module._grid.Length; i++)
            {
                int o;
                if (!int.TryParse(numbers[i], out o))
                    continue;

                o %= Module.Modulus;

                while(true)
                {
                    if(time * (Mathf.Pow(Module.GridSize / 16f, 1.5f) * Module.Modulus) <= p + 1)
                    {
                        yield return "trycancel";
                        if (p > 0)
                        {
                            Module._grid[i].ApplyChanges(p - 1);
                            Module._grid[i].Button.OnInteract();
                        }
                        p = 0;
                        time += Time.deltaTime;
                        if (o <= 0)
                            break;
                        continue;
                    }

                    while(o > 0 && time * (Mathf.Pow(Module.GridSize / 16f, 1.5f) * Module.Modulus) > p + 1)
                    {
                        p++;
                        o--;
                    }
                    time = 0;
                }
            }
        }
        else if (Regex.IsMatch(command, @"(([A-Z])+([0-9])+\s*)+"))
        {
            string[] presses = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < presses.Length; i++)
            {
                string column = Regex.Match(presses[i], "[A-Z]+").Value;
                int c = 0;

                for (int j = 0; j < column.Length; j++)
                    c += (column[j] - 'A' + (j == column.Length - 1 ? 0 : 1)) * (int)Mathf.Pow(26, column.Length - 1 - j);

                int r = int.Parse(Regex.Match(presses[i], "[0-9]+").Value);

                yield return null;
                if (c + (r - 1) * Module.Width >= Module.GridSize)
                    yield return "sendtochaterror That button does not exist on this grid. Try again.";
                else
                    Module._grid[c + (r - 1) * Module.Width].Button.OnInteract();
                yield return new WaitForSeconds(.1f);
                yield return "trycancel";
            }
        }
        else if(Regex.IsMatch(command, @"(RESIZE|SETSIZE)\s+[0-9]+\s+[0-9]+"))
        {
            if (Module.IsMissionSettingsSet)
                yield return "sendtochat The mission has set its own parameters. Grid size changes are disallowed.";
            else if (Module.IsMission() && !Application.isEditor)
            {
                var match = Regex.Match(command, @"(RESIZE|SETSIZE)\s+([0-9]+)\s+([0-9]+)");

                int h = int.Parse(match.Groups[2].Value);
                int w = int.Parse(match.Groups[3].Value);

                if (h * w >= 16)
                {
                    Module.Height = int.Parse(match.Groups[2].Value);
                    Module.Width = int.Parse(match.Groups[3].Value);

                    Module.MinAffected = Mathf.Clamp(Module.Settings.MinAffected, Module.GridSize / 3, Module.GridSize);
                    Module.MaxAffected = Mathf.Clamp(Module.Settings.MaxAffected, Mathf.Max((int)(Module.GridSize / 1.5f), Module.MinAffected), Module.GridSize);

                    Module.GenerateGrid(Module.Modulus);
                }
                else
                    yield return "sendtochaterror In missions, the grid size cannot be set to lower than the default 16 tiles.";
            }
            else
            {
                var match = Regex.Match(command, @"(RESIZE|SETSIZE)\s+([0-9]+)\s+([0-9]+)");

                int h = int.Parse(match.Groups[2].Value);
                int w = int.Parse(match.Groups[3].Value);

                if (h * w >= 9)
                {
                    Module.Height = int.Parse(match.Groups[2].Value);
                    Module.Width = int.Parse(match.Groups[3].Value);

                    Module.MinAffected = Mathf.Clamp(Module.Settings.MinAffected, Module.GridSize / 3, Module.GridSize);
                    Module.MaxAffected = Mathf.Clamp(Module.Settings.MaxAffected, Mathf.Max((int)(Module.GridSize / 1.5f), Module.MinAffected), Module.GridSize);

                    Module.GenerateGrid(Module.Modulus);
                    yield return AwardPointsOnSolve(GetOffsetScore(Module.GridSize, Module.Modulus));
                }
                else
                    yield return "sendtochaterror The grid size must be at least 9 tiles.";
            }
        }
        else
            yield return "sendtochaterror The module did not detect any valid command formats. Check your command.";
    }

    private static int GCD(int a, int b)
    {
        if (b == 0) return a;

        return GCD(b, a % b);
    }

    private static int Totient(int x)
    {
        int cnt = 0;

        for (int i = 1; i < x; i++)
            if (GCD(x, i) == 1)
                cnt++;

        return cnt;
    }

    private static int GetOffsetScore(int gridSize, int modulus)
    {
        return Mathf.RoundToInt(
            5 * (gridSize - 3 * Mathf.Sqrt(gridSize) + 8)
            * (0.1f * Totient(modulus) / modulus + 1.6f * (1 - (float)Totient(modulus) / modulus))
        ) - 60;
    }

    public void UpdateHelpMessage(int gridSize)
    {
        if (Module.Modulus <= 10)
        {
            Help = "<!{0} cycle [delay]> to press each tile in reading order once, with given delay; the delay can be in decimal form, e.g. 2.47. <!{0} <" + Module._grid.Length.ToString() + " digits>> to construct the array for which to press each position in reading order that number of times. <!{0} [coordinate]> to press that specific coordinate, with the column being specified by letters, with the column after Z being AA, and the row being specified by a number. The first command type is not chainable, nor can you use both types of commands in one, however the second command type can have multiple coordinates separated by spaces. <!{0} <" + Module._grid.Length.ToString() + " numbers separated by spaces>> to also construct a more precise array for which to press the buttons, similar to the first command. <!{0} (resize|setsize) [height] [width]> to resize the grid, and reset all values. However, width * height must be at least 9. This command is disallowed in a mission.";
        }
        else
        {
            Help = "<!{0} cycle [delay]> to press each tile in reading order once, with given delay; the delay can be in decimal form, e.g. 2.47. <!{0} [coordinate]> to press that specific coordinate, with the column being specified by letters, with the column after Z being AA, and the row being specified by a number. You cannot use both types of commands in one, however the second command type can have multiple coordinates separated by spaces. <!{0} <" + Module._grid.Length.ToString() + " numbers separated by spaces>> to construct a precise array for which to press the buttons. <!{0} (resize|setsize) [height] [width]> to resize the grid, and reset all values. However, width * height must be at least 9. This command is disallowed in a mission.";
        }
        TwitchHelpMessage = Help;
    }
}
