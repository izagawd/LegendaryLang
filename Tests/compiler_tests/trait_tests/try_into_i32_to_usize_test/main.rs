use Std.Ops.TryInto;
use Std.Primitive.TryCastPrimitive;

fn main() -> i32 {
    let x: i32 = 100;
    let result: Option(usize) = x.try_into();
    match result {
        Option.Some(val) => 1,
        Option.None => 0
    }
}
