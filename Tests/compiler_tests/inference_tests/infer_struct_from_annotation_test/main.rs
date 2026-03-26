struct Wrapper<T> {
    val: T
}

fn main() -> i32 {
    let a : Wrapper<i32> = Wrapper { val = 5 };
    a.val
}
