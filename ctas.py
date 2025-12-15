import pandas as pd
import numpy as np

def calc_p(file1_path, file2_path):
    df1 = pd.read_csv(file1_path)
    df2 = pd.read_csv(file2_path)

    n = min(len(df1), len(df2))
    df1 = df1.head(n)
    df2 = df2.head(n)

    x1, z1 = df1["x"], df1["z"]
    x2, z2 = df2["x"], df2["z"]

    # d_k = sqrt((x - x_hat)^2 + (y - y_hat)^2)
    dk = np.sqrt((x1 - x2) ** 2 + (z1 - z2) ** 2)

    # ∑ sqrt(x_hat^2 + y_hat^2) / n
    denominator = np.mean(np.sqrt(x2 ** 2 + z2 ** 2))

    # p_k = 1 / (1 + d_k / denominator)
    pk = 1 / (1 + dk / denominator)

    p = pk.mean()

    return p


if __name__ == "__main__":
    file1 = "1.csv"
    file2 = "2.csv"
    p_value = calc_p(file1, file2)
    print(f"ファイルの平均類似度は: {p_value:.6f}")
