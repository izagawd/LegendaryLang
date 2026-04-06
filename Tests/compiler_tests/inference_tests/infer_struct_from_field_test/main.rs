struct Wrapper(T:! type) {
    val: T
}

fn main() -> i32 {
    let a = make Wrapper { val : 42 };
    a.val
}
