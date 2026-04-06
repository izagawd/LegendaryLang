use Std.Core.Ops.Add;
struct Wrapper(T:! type) {
    val: T
}

impl[T:! Copy] Copy for Wrapper(T) {}

impl[T:! Add(T, Output = T) + Copy] Add(Wrapper(T)) for Wrapper(T) {
    type Output = Wrapper(T);
    fn Add(lhs: Wrapper(T), rhs: Wrapper(T)) -> Wrapper(T) {
        make Wrapper { val : lhs.val + rhs.val }
    }
}

fn main() -> i32 {
    let a = make Wrapper { val : 3 };
    let b = make Wrapper { val : 7 };
    let c = (Wrapper(i32) as Add(Wrapper(i32))).Add(a, b);
    c.val
}
