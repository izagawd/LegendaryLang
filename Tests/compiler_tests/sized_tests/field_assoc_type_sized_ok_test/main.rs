use Std.Ops.Add;

struct Result(T:! Add(T)) {
    val: (T as Add(T)).Output
}

fn main() -> i32 {
    let r = make Result { val: 1 + 2 };
    r.val
}
