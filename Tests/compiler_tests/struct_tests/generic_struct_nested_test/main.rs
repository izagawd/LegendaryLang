struct Wrapper(T:! type) {
    val: T
}

fn main() -> i32 {
    let inner = make Wrapper(i32) { val : 7 };
    let outer = Wrapper(Wrapper(i32)) { val : inner };
    outer.val.val
}
