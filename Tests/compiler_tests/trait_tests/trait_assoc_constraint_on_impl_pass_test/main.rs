use Std.Core.Ops.Add;
struct Wrapper(T:! type) {
    val: T
}

trait Summable {
    fn sum(a: Self, b: Self) -> Self;
}

impl[T:! Add(T, Output = T) + Copy] Summable for Wrapper(T) {
    fn sum(a: Wrapper(T), b: Wrapper(T)) -> Wrapper(T) {
        make Wrapper { val: a.val + b.val }
    }
}

impl Copy for Wrapper(i32) {}

fn main() -> i32 {
    let a = make Wrapper { val: 10 };
    let b = make Wrapper { val: 32 };
    let c = Wrapper(i32).sum(a, b);
    c.val
}
