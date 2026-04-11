struct Wrapper(T:! Sized) {
    val: T
}

fn main() -> i32 {
    let w = make Wrapper(i32) { val : 42 };
    w.val
}
