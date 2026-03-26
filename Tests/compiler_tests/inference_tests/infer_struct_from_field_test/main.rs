struct Wrapper<T> {
    val: T
}

fn main() -> i32 {
    let a = Wrapper { val = 42 };
    a.val
}
