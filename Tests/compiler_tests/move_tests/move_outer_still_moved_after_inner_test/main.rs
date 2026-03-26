struct Idk {
    val: i32
}

fn main() -> i32 {
    let a = Idk { val = 4 };
    let b = a;
    {
        let c = Idk { val = 10 };
    }
    a.val
}
