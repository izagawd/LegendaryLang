struct Wrapper(T:! type) {
    val: T
}

fn extract(T:! Copy, w: Wrapper(T)) -> T {
    w.val
}

fn main() -> i32 {
    let w = make Wrapper(i32) { val : 77 };
    extract(i32, w)
}
