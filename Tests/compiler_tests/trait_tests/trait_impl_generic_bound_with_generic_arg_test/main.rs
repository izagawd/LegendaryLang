struct Wrapper<T> {
    val: T
}

impl<T: Copy> Copy for Wrapper<T> {}

impl<T: Add<T> + Copy> Add<Wrapper<T>> for Wrapper<T> {
    type Output = Wrapper<T>;
    fn add(lhs: Wrapper<T>, rhs: Wrapper<T>) -> Wrapper<T> {
        Wrapper { val = lhs.val + rhs.val }
    }
}

fn main() -> i32 {
    let a = Wrapper { val = 3 };
    let b = Wrapper { val = 7 };
    let c = <Wrapper<i32> as Add<Wrapper<i32>>>::add(a, b);
    c.val
}
