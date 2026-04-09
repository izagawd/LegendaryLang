struct Wrapper(T:! type) {
    val: T
}

impl[T:! Copy] Copy for Wrapper(T) {}

fn main() -> i32 {
    let a = make Wrapper(i32) { val : 5 };
    let b = a;
    a.val + b.val
}
