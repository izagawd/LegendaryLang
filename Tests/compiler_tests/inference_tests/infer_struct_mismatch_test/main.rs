struct Wrapper<T> {
    val: T
}

fn main() -> i32 {
    let a : Wrapper<bool> = Wrapper { val = 5 };
    3
}
