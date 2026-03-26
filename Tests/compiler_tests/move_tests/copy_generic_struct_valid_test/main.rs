struct Wrapper<T> {
    val: T
}

impl<T: Copy> Copy for Wrapper<T> {}

fn main() -> i32 {
    let a = Wrapper::<i32> { val = 5 };
    let b = a;
    a.val + b.val
}
