using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace Nuclear.Airkraft.Kernel
{
    /// <summary>
    /// Notification provider
    /// </summary>
    public static class Notificator
    {
        // TODO: Implementation
    }

    /*
     * Krileの通知を一手に受け持たせるクラス．
     * 各種オペレーションの実行結果やTwitterステータス・アクティビティについての情報をすべて集積し，
     * 必要とするViewModelなどにプッシュ配信できるようにする．
     * おそらく，Subject<T>を持ちいてIObservable<T>で公開してVMがそれをSubscribeする形になるかと思われ
     * 失敗したオペレーションに関しては別個で再実行できるようにしたいのでここでは取り扱わない．
     * (OperationDispatcherが面倒を見てくれるはず)
     * 
     * 現時点で考えられる通知としては
     * ・ツイート
     * ツイートの失敗，成功，フォールバック
     * ・ステータス
     * 新規mention，DM，エゴサーチ引っ掛かりなど
     * ・アクティビティ
     * ふぁぼ，RT，爆撃など
     * 
     * 爆撃耐性が欲しいので，ステータスやアクティビティに関しては統合フィルタリングを行いたい
     * Krile Mystiqueのインビジブルなんとかは少々改良が必要なのでアルゴリズムを詰めたいところ
     * ↑こいつの積んでるやつの問題としては
     * ・爆撃初期のイベントを通してしまうため，複垢爆撃などにはO(n)で爆撃を受けてしまう
     * ・長時間爆撃を受け続けていると定期的にインビジブル通知が入ってしまう
     * あたり．いろいろ考える．
     * 
     */
}
