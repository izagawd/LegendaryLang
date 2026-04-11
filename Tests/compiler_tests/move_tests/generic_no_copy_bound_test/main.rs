struct Wrapper {
    val: i32
}

fn use_twice(T:! Sized, a: T, b: T) -> i32 {
    5
}

fn main() -> i32 {
    let w = make Wrapper { val : 10 };
    use_twice(Wrapper, w, w)
}
