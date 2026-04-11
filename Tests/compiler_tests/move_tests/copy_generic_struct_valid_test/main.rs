struct Wrapper(T:! Sized) {
    val: T
}

impl[T:! Sized +Copy] Copy for Wrapper(T) {}

fn main() -> i32 {
    let a = make Wrapper(i32) { val : 5 };
    let b = a;
    a.val + b.val
}
