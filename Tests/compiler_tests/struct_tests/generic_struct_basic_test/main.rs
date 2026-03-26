struct Wrapper<T> {
    val: T
}

fn main() -> i32 {
    let w = Wrapper::<i32> { val = 42 };
    w.val
}
