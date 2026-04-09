struct Wrapper(T:! type) {
    val: T
}

fn main() -> i32 {
    let a : Wrapper(bool) = make Wrapper { val : 5 };
    3
}
