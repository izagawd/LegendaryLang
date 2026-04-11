struct Wrapper(T:! Sized) {
    val: T
}

fn main() -> i32 {
    let a : Wrapper(i32) = make Wrapper { val : 5 };
    a.val
}
