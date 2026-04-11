struct Wrapper(T:! Sized) {
    val: T
}

fn main() -> i32 {
    let a : Wrapper(bool) = make Wrapper { val : 5 };
    3
}
