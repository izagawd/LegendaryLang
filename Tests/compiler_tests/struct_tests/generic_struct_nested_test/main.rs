struct Wrapper(T:! Sized) {
    val: T
}

fn main() -> i32 {
    let inner = make Wrapper(i32) { val : 7 };
    let outer = make Wrapper(Wrapper(i32)) { val : inner };
    outer.val.val
}
